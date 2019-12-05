using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using MDG.Common.Datastructures;
using Improbable;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using PositionSchema = MdgSchema.Common.Position;
using MDG.Common.Systems.Collision;
using MDG.ScriptableObjects.Game;

namespace MDG.Common.Systems.Position
{
    /// <summary>
    /// System that runs on server, manages the spatial partitioned collection of all entities with entity
    /// transform component. It also does ALL updates to position. All position updates must request this system.
    /// Latter might not be good idea. All just change velocity, but it only ever actually applies when hits this system.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateBefore(typeof(CollisionDetectionSystem))]
    [AlwaysUpdateSystem]
    public class PositionSystem : ComponentSystem
    {
        struct UpdatePayload
        {
            public EntityId EntityUpdating;
            public Vector3f NewPosition;
        }
        JobHandle? spatialPosUpdateJobHandle;

        EntitySystem entitySystem;
        WorkerSystem worker;
        CommandSystem commandSystem;

        EntityQuery updateSpatialPositionQuery;
        EntityQuery toAddToTreeQuery;
        EntityQuery applyVelocityQuery;

        QuadTree spatialPartitioning;
        // For batch updating tree.
        NativeQueue<UpdatePayload> updateQueue;
       

        Queue<EntityId> toPruneOff;

        // For updating spatialOS position component.
        Dictionary<EntityId, Vector3f> entitiesThatMovedRegionBuffer;
        const int maxPositionUpdateBuffer = 10;

        readonly int shakesPerFrame = 5;

        #region Interfacing methods
        public int GetRegionCount()
        {
            return spatialPartitioning.GetNumberRegions();
        }

        public List<QuadNode> querySpatialPartition(Vector3f position)
        {
            return spatialPartitioning.FindEntities(position);
        }

        public NativeList<QuadNode> querySpatialPartition(Vector3f position, Allocator allocator)
        {
            return spatialPartitioning.FindEntities(position, allocator);
        }

        // Returns collective region  that the entityId belongs to.
        public QuadNode? querySpatialPartition(EntityId entityId)
        {
            return spatialPartitioning.FindEntity(entityId);
        }

        #endregion
        protected override void OnCreate()
        {
            base.OnCreate();
            worker = World.GetExistingSystem<WorkerSystem>();

            entitiesThatMovedRegionBuffer = new Dictionary<EntityId, Vector3f>();
            updateQueue = new NativeQueue<UpdatePayload>(Allocator.Persistent);

            commandSystem = World.GetExistingSystem<CommandSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();

            toPruneOff = new Queue<EntityId>();

            GameConfig gameConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseGameConfig") as GameConfig;


            spatialPartitioning = new QuadTree(gameConfig.CapicityPerRegion,
                HelperFunctions.Vector3fFromUnityVector(gameConfig.WorldDimensions), Vector3f.Zero);

            spatialPartitioning.OnMovedRegions += OnMovedRegion;


            updateSpatialPositionQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Improbable.Position.ComponentAuthority>(),
                ComponentType.ReadWrite<Improbable.Position.Component>()
                );
            updateSpatialPositionQuery.SetFilter(Improbable.Position.ComponentAuthority.Authoritative);

            toAddToTreeQuery = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );
            applyVelocityQuery = GetEntityQuery(
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.AngularVelocity.Component>(),
                ComponentType.ReadWrite<EntityTransform.Component>(),
                ComponentType.ReadOnly<EntityTransform.ComponentAuthority>()
                );
            applyVelocityQuery.SetFilter(EntityTransform.ComponentAuthority.Authoritative);
        }

        protected override void OnDestroy()
        {
            if (updateQueue.IsCreated)
            {
                updateQueue.Dispose();
            }
            base.OnDestroy();
        }

        #region Event Handlers
        private void OnMovedRegion(QuadNode quadNode)
        {
            if (entitiesThatMovedRegionBuffer.TryGetValue(quadNode.entityId, out Vector3f updatedPosition))
            {
                entitiesThatMovedRegionBuffer[quadNode.entityId] = quadNode.position;
            }
            else
            {
                entitiesThatMovedRegionBuffer.Add(quadNode.entityId, quadNode.position);
            }
        }


        #endregion


        #region Jobs

        // Only update if enters new region, etc.
        struct UpdateSpatialPositionJob : IJobForEach<SpatialEntityId, Improbable.Position.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, Vector3f> toUpdate;
            // Only run this job every few frames. Make when run more later. maybe if update queue above certain size, etc.
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, ref Improbable.Position.Component positionComponent)
            {
                if (toUpdate.TryGetValue(spatialEntityId.EntityId, out Vector3f newPos))
                {
                    positionComponent.Coords = new Coordinates(newPos.X, newPos.Y, newPos.Z);
                }
            }
        }


        struct ApplyVelocityJob : IJobForEach<SpatialEntityId, PositionSchema.LinearVelocity.Component, PositionSchema.AngularVelocity.Component,
            EntityTransform.Component> {

            public float deltaTime;
            [WriteOnly]
            public NativeQueue<UpdatePayload>.ParallelWriter updateQueue;

            public void Execute([ReadOnly] ref SpatialEntityId entityIdComponent, [ReadOnly] ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
                [ReadOnly] ref PositionSchema.AngularVelocity.Component angularVelocityComponent,
                ref EntityTransform.Component entityTransform)
            {
                // Add extra speed to this.
                entityTransform.Position += HelperFunctions.Normalize(linearVelocityComponent.Velocity) * deltaTime * 100.0f;
                // entityTransform.Position = new Vector3f(entityTransform.Position.X, 0, entityTransform.Position.Z);
                entityTransform.Rotation += angularVelocityComponent.AngularVelocity * deltaTime;
                if (!linearVelocityComponent.Velocity.Equals(Vector3f.Zero))
                {
                    updateQueue.Enqueue(new UpdatePayload
                    {
                        EntityUpdating = entityIdComponent.EntityId,
                        NewPosition = entityTransform.Position
                    });
                }
            }
        }

        #endregion

        #region Internal Methods
        protected override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            ApplyVelocityJob applyVelocityJob = new ApplyVelocityJob
            {
                deltaTime = deltaTime,
                updateQueue = updateQueue.AsParallelWriter()
            };
            // Run apply velocity job in background while queue up removed entities to prune and transfer from buffer
            // entities that ahve moved regions to native map.
            JobHandle applyVelocityHandle = applyVelocityJob.Schedule(applyVelocityQuery);

            // Getting entities removed to queue up to prune off of quad tree.
            List<EntityId> removedEntities = entitySystem.GetEntitiesRemoved();
            foreach (EntityId removedEntity in removedEntities)
            {
                toPruneOff.Enqueue(removedEntity);
            }

            AddNewEntitiesToQuadTree();
            NativeHashMap<EntityId, Vector3f> entitiesThatMovedRegions = new NativeHashMap<EntityId, Vector3f>(applyVelocityQuery.CalculateEntityCount(), Allocator.TempJob);
            foreach (KeyValuePair<EntityId, Vector3f> positionUpdate in entitiesThatMovedRegionBuffer)
            {
                entitiesThatMovedRegions.TryAdd(positionUpdate.Key, positionUpdate.Value);
            }
            applyVelocityHandle.Complete();
            if (entitiesThatMovedRegions.Length > 0)
            {
                UpdateSpatialPositionJob updateSpatialPositionJob = new UpdateSpatialPositionJob
                {
                    toUpdate = entitiesThatMovedRegions
                };
                // Runs updating spatialOS position with entity transform in background while update entities in tree
                // and remove from quad tree removed entities.
                spatialPosUpdateJobHandle = updateSpatialPositionJob.Schedule(updateSpatialPositionQuery);
            }
            UpdateEntitiesInTree();
            ShakeQuadTree();
            if (spatialPosUpdateJobHandle.HasValue)
            {
                spatialPosUpdateJobHandle.Value.Complete();
            }
            entitiesThatMovedRegions.Dispose();
        }

        // Iterates through newly added entities and inserts into quad tree.
        // Main issue here, is position not updated yet. So will be adding the entity, then immediemtly moving it next frame.
        private void AddNewEntitiesToQuadTree()
        {
            // For jobifying this system later, move this to job then dequeue.
            Entities.With(toAddToTreeQuery).ForEach((ref SpatialEntityId spatialEntityId, ref EntityTransform.Component entityTransform) =>
            {
                if (!spatialPartitioning.FindEntity(spatialEntityId.EntityId).HasValue)
                {
                    spatialPartitioning.Insert(spatialEntityId.EntityId, entityTransform.Position);
                }
            });
        }

        // Runs through update queues and updates them in batches in quad tree.
        // over a couple frames is fine.
        private void UpdateEntitiesInTree()
        {
            // For each entityId to update, first check using the position to update to if still within region.
            while (updateQueue.IsCreated && updateQueue.TryDequeue(out UpdatePayload updatePayload))
            {
                spatialPartitioning.MoveEntity(updatePayload.EntityUpdating, updatePayload.NewPosition);
            }
        }

        // Prunes tree of any entities in quad tree that are no longer active.
        // thus reducing size. Maybe down road will clean to close any subdivisions.
        // or keep subdivided, but still prune. Worry about this later.
        private void ShakeQuadTree()
        {
            // Shaking is expensive, could do smarter, and prune per region, make entities to remove also subdivided.
            int shakesThisFrame = 0;
            while (shakesThisFrame < shakesPerFrame && toPruneOff.Count > 0)
            {
                // For now it's fine, like this.
                EntityId toRemove = toPruneOff.Dequeue();
                spatialPartitioning.Remove(toRemove);
                shakesThisFrame += 1;
            }
        }
        #endregion
    }
}