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

        EntitySystem entitySystem;
        WorkerSystem worker;
        CommandSystem commandSystem;
        EntityQuery toAddToTreeQuery;
        EntityQuery applyVelocityQuery;
        public Vector3f RootDimensions { get; } = new Vector3f(1000, 0, 1000);
        public int RegionCapacity { get; } = 5;

        QuadTree spatialPartitioning;
        // For batch updating tree.
        NativeQueue<UpdatePayload> updateQueue;

        Queue<EntityId> toPruneOff;

        readonly int shakesPerFrame;


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
        protected override void OnCreate()
        {
            base.OnCreate();
            worker = World.GetExistingSystem<WorkerSystem>();
            updateQueue = new NativeQueue<UpdatePayload>(Allocator.Persistent);
            commandSystem = World.GetExistingSystem<CommandSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            toPruneOff = new Queue<EntityId>();
            spatialPartitioning = new QuadTree(RegionCapacity, RootDimensions, new Vector3f(0,0,0));
            toAddToTreeQuery = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );
            applyVelocityQuery = GetEntityQuery(
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
                entityTransform.Position = new Vector3f(entityTransform.Position.X, 0, entityTransform.Position.Z);
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
        /* 
        struct UpdatePartitionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeList<UpdatePayload> updatePayload;
            public QuadTree quadTree;

            public void Execute(int index)
            {
                quadTree.MoveEntity(updatePayload[index].EntityUpdating, updatePayload[index].NewPosition);
            }
        }
        */

        protected override void OnUpdate()
        {
            // This should handle all requests for updates to position.
            // as well as running job to update position via velocity.

            #region Running Jobs
            float deltaTime = Time.deltaTime;
            ApplyVelocityJob applyVelocityJob = new ApplyVelocityJob
            {
                deltaTime = deltaTime,
                updateQueue = updateQueue.AsParallelWriter()
            };
            applyVelocityJob.Schedule(applyVelocityQuery).Complete();

           /* UpdatePartitionJob updatePartitionJob = new UpdatePartitionJob
            {
                quadTree = quadTree,
                updatePayload = updateQueue
            };
            updatePartitionJob.Schedule(updateQueue.Length, 1).Complete();*/

            #endregion

            #region Quad Tree operations
            UpdateEntitiesInTree();
            AddNewEntitiesToQuadTree();
            // Prepping shake queueing up entities removed this frame.
            List<EntityId> removedEntities = entitySystem.GetEntitiesRemoved();
            foreach (EntityId removedEntity in removedEntities)
            {
                toPruneOff.Enqueue(removedEntity);
            }
            ShakeQuadTree();
            #endregion
        }

        // Iterates through newly added entities and inserts into quad tree.
        // Main issue here, is position not updated yet. So will be adding the entity, then immediemtly moving it next frame.
        private void AddNewEntitiesToQuadTree()
        {
            Entities.With(toAddToTreeQuery).ForEach((ref SpatialEntityId spatialEntityId, ref EntityTransform.Component entityTransform) =>
            {
                spatialPartitioning.Insert(spatialEntityId.EntityId, entityTransform.Position);
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
    }
}