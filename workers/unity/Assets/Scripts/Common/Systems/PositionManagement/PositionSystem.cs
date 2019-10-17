﻿using System.Collections;
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
namespace MDG.Common.Systems.Position
{
    /// <summary>
    /// System that runs on server, manages the spatial partitioned collection of all entities with entity
    /// transform component. It also does ALL updates to position. All position updates must request this system.
    /// Latter might not be good idea. All just change velocity, but it only ever actually applies when hits this system.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class PositionSystem : ComponentSystem
    {
        struct UpdatePayload
        {
            public EntityId entityUpdating;
            public Vector3f newPosition;
        }

        EntitySystem entitySystem;
        WorkerSystem worker;
        CommandSystem commandSystem;
        EntityQuery toAddToTreeQuery;
        EntityQuery applyVelocityQuery;
        // This will be dimensions of world down line, injected here.
        readonly Vector3f initialDimensions = new Vector3f(50, 0, 50);
        readonly int regionCapacity = 2;

        public Vector3f RootDimensions { get { return initialDimensions; } }
        public int RegionCapacity { get { return regionCapacity; } }

        QuadTree quadTree;
        NativeQueue<UpdatePayload> updateQueue;

        Queue<EntityId> toPruneOff;

        readonly int shakesPerFrame;
        
        public List<QuadNode> querySpatialPartition(Vector3f position)
        {
            return quadTree.FindEntities(position);
        }

        // Returns collective region  that the entityId belongs to.
        public QuadNode querySpatialPartition(EntityId entityId)
        {
            return quadTree.FindEntity(entityId);
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            worker = World.GetExistingSystem<WorkerSystem>();
            updateQueue = new NativeQueue<UpdatePayload>(Allocator.Persistent);
            commandSystem = World.GetExistingSystem<CommandSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            toPruneOff = new Queue<EntityId>();
            quadTree = new QuadTree(regionCapacity, initialDimensions, new Vector3f(initialDimensions.X / 2, 0, initialDimensions.Z / 2));
            toAddToTreeQuery = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );
            applyVelocityQuery = GetEntityQuery(
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


        struct ApplyVelocityJob : IJobForEach<PositionSchema.LinearVelocity.Component, PositionSchema.AngularVelocity.Component,
            EntityTransform.Component>
        {
            public float deltaTime;
            public void Execute([ReadOnly] ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
                [ReadOnly] ref PositionSchema.AngularVelocity.Component angularVelocityComponent,
                [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                entityTransform.Position += linearVelocityComponent.Velocity * deltaTime;
                entityTransform.Rotation += angularVelocityComponent.AngularVelocity * deltaTime;
            }
        }


        protected override void OnUpdate()
        {
            // This should handle all requests for updates to position.
            // as well as running job to update position via velocity.

            #region Running Jobs
            float deltaTime = Time.deltaTime;
            ApplyVelocityJob applyVelocityJob = new ApplyVelocityJob
            {
                deltaTime = deltaTime
            };
            applyVelocityJob.Run(applyVelocityQuery);
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
                quadTree.Insert(spatialEntityId.EntityId, entityTransform.Position);
            });
        }

        // Runs through update queues and updates them in batches in quad tree.
        // over a couple frames is fine.
        private void UpdateEntitiesInTree()
        {
            // For each entityId to update, first check using the position to update to if still within region.
            while (updateQueue.IsCreated && updateQueue.TryDequeue(out UpdatePayload updatePayload))
            {
                List<QuadNode> entitiesInRegion = quadTree.FindEntities(updatePayload.newPosition);
                QuadNode lastRecord = entitiesInRegion.Find((QuadNode quadNode) =>
                {
                    return quadNode.entityId.Equals(updatePayload.entityUpdating);
                });

                // Then no need for update, still in same region.
                if (lastRecord != null)
                {
                    continue;
                }
                // Otherwise get entity from old region.
                lastRecord = quadTree.FindEntity(updatePayload.entityUpdating);

                //And remove from last and insert in new.
                quadTree.MoveEntity(updatePayload.entityUpdating, lastRecord.position, updatePayload.newPosition);
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
                quadTree.Remove(toRemove);
                shakesThisFrame += 1;
            }
        }
    }
}