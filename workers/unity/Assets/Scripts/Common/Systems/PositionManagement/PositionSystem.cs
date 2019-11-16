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

        struct BatchInformation
        {
            public Vector3f ChangedPosition;
            // Frames passed with update to position.
            public int updatesPending;
        }

        JobHandle? spatialPosUpdateJobHandle;

        EntitySystem entitySystem;
        WorkerSystem worker;
        CommandSystem commandSystem;

        EntityQuery updateSpatialPositionQuery;
        EntityQuery toAddToTreeQuery;
        EntityQuery applyVelocityQuery;

        public Vector3f RootDimensions { get; } = new Vector3f(1000, 0, 1000);
        public int RegionCapacity { get; } = 50;

        QuadTree spatialPartitioning;
        // For batch updating tree.
        NativeQueue<UpdatePayload> updateQueue;
       

        Queue<EntityId> toPruneOff;

        // For updating spatialOS position component.
        NativeHashMap<EntityId, Vector3f> entitiesThatMovedRegions;

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

            entitiesThatMovedRegions = new NativeHashMap<EntityId, Vector3f>(1000, Allocator.Persistent);
            updateQueue = new NativeQueue<UpdatePayload>(Allocator.Persistent);

            commandSystem = World.GetExistingSystem<CommandSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();

            toPruneOff = new Queue<EntityId>();
            spatialPartitioning = new QuadTree(RegionCapacity, RootDimensions, new Vector3f(0,0,0));
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

            entitiesThatMovedRegions.Dispose();
        }

        #region Event Handlers
        private void OnMovedRegion(QuadNode quadNode)
        {
            if (entitiesThatMovedRegions.TryGetValue(quadNode.entityId, out Vector3f updatedPosition))
            {
                entitiesThatMovedRegions[quadNode.entityId] = quadNode.position;
            }
            entitiesThatMovedRegions.TryAdd(quadNode.entityId, quadNode.position);
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
                entityTransform.Position += linearVelocityComponent.Velocity * deltaTime;
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

            if (spatialPosUpdateJobHandle.HasValue)
            {
                spatialPosUpdateJobHandle.Value.Complete();
                entitiesThatMovedRegions.Clear();
            }

            JobHandle applyVelocityHandle = applyVelocityJob.Schedule(applyVelocityQuery);

            // Getting entities removed to queue up to prune off of quad tree.
            List<EntityId> removedEntities = entitySystem.GetEntitiesRemoved();
            foreach (EntityId removedEntity in removedEntities)
            {
                toPruneOff.Enqueue(removedEntity);
            }
            ShakeQuadTree();

            AddNewEntitiesToQuadTree();

            applyVelocityHandle.Complete();
            UpdateEntitiesInTree();


            // Update may cause updates to hash map at same time as job.. ACTUALLY that is fine cause job only read read only.
            if (entitiesThatMovedRegions.Length > 0)
            {
                UpdateSpatialPositionJob updateSpatialPositionJob = new UpdateSpatialPositionJob
                {
                    toUpdate = entitiesThatMovedRegions 
                };
                spatialPosUpdateJobHandle = updateSpatialPositionJob.Schedule(updateSpatialPositionQuery);
                spatialPosUpdateJobHandle.Value.Complete();
                spatialPosUpdateJobHandle = null;
            }



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
                //And remove from last and insert in new.
                try
                {
                    spatialPartitioning.MoveEntity(updatePayload.EntityUpdating, updatePayload.NewPosition);
                }
                catch (System.Exception error)
                {
                    Debug.Log(error);
                }
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