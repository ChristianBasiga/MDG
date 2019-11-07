using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using CollisionSchema = MdgSchema.Common.Collision;
using MDG.Common.Systems.Position;
using Improbable.Gdk.Core;
using MDG.Common.Datastructures;
using Improbable;
using MdgSchema.Common;

namespace MDG.Common.Systems.Collision
{
    /// <summary>
    /// Sits on the server side, and sets collision component for all entities to then query and react upon.
    /// It also sends events.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateBefore(typeof(CollisionHandlerSystem))]
    [UpdateAfter(typeof(PositionSystem))]
    [AlwaysUpdateSystem]
    public class CollisionDetectionSystem : ComponentSystem
    {

        PositionSystem positionSystem;
        WorkerSystem workerSystem;
        ComponentUpdateSystem componentUpdateSystem;
        EntityQuery checkCollisionGroup;
        EntityQuery updateCollisionGroup;

        readonly int collisionBuffer = 1000;

        public struct ColliderCheck
        {
            public EntityId entityId;
            public Vector3f dimensions;
            public Vector3f center;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            positionSystem = World.GetExistingSystem<PositionSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            updateCollisionGroup = GetEntityQuery(
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                ComponentType.ReadWrite<CollisionSchema.Collision.Component>(),
                ComponentType.ReadOnly<CollisionSchema.Collision.ComponentAuthority>()
                );

            updateCollisionGroup.SetFilter(CollisionSchema.Collision.ComponentAuthority.Authoritative);

            checkCollisionGroup = GetEntityQuery(
                    ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                    ComponentType.ReadOnly<SpatialEntityId>(),
                    ComponentType.ReadOnly<EntityTransform.Component>(),
                    ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>()
                );
        }

        // If didn't jobify it. Maybe could jobify subloop of it??
        private struct CheckCollisionJob : IJobForEach<SpatialEntityId, EntityTransform.Component, CollisionSchema.BoxCollider.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> entityIdToRegionIndex;
            [ReadOnly]
            public NativeHashMap<int, NativeList<ColliderCheck>> regionIndexToColliders;

            [WriteOnly]
            public NativeHashMap<EntityId, NativeList<CollisionSchema.CollisionPoint>>.ParallelWriter entityIdToCollisions;

            // Need to add queue of events need to send for each collision.
            // if still wanna do that. That could be resolved frame after this completes though.
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref EntityTransform.Component entityTransform, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider)
            {
                NativeList<CollisionSchema.CollisionPoint> collisionPoints = new NativeList<CollisionSchema.CollisionPoint>(entityIdToRegionIndex.Length, Allocator.TempJob);
                if (entityIdToRegionIndex.TryGetValue(spatialEntityId.EntityId, out int regionIndex))
                {
                    if (regionIndexToColliders.TryGetValue(regionIndex, out NativeList<ColliderCheck> collidersToCheckAgainst))
                    {
                        foreach(ColliderCheck colliderCheck in collidersToCheckAgainst)
                        {
                            if (HelperFunctions.Intersect(
                                colliderCheck.center, colliderCheck.dimensions, 
                                entityTransform.Position - boxCollider.Position, boxCollider.Dimensions))
                            {
                                collisionPoints.Add(new CollisionSchema.CollisionPoint
                                {
                                    CollidingWith = colliderCheck.entityId,
                                    Distance = entityTransform.Position - colliderCheck.center
                                });
                            }
                        }
                    }
                }
                entityIdToCollisions.TryAdd(spatialEntityId.EntityId, collisionPoints);
            }
        }
      
        protected override void OnUpdate()
        {
            // Quick collision detection, zero optimization/
            Entities.With(updateCollisionGroup).ForEach((ref SpatialEntityId spatialEntityId, ref EntityTransform.Component entityTransform, 
                ref CollisionSchema.BoxCollider.Component boxCollider, ref CollisionSchema.Collision.Component collisionComponent) =>
            {
                Dictionary<EntityId, CollisionSchema.CollisionPoint> previousCollisions = collisionComponent.Collisions;
                foreach(var key in previousCollisions.Keys)
                {

                }
                Dictionary<EntityId, CollisionSchema.CollisionPoint> newCollisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
                List<QuadNode> potentialCollisions = positionSystem.querySpatialPartition(entityTransform.Position);
                foreach (QuadNode potentialCollision in potentialCollisions)
                {
                    if (workerSystem.TryGetEntity(potentialCollision.entityId, out Entity entity) && EntityManager.HasComponent<CollisionSchema.BoxCollider.Component>(entity))
                    {
                        CollisionSchema.BoxCollider.Component otherBoxCollider = EntityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(entity);
                        if (!potentialCollision.entityId.Equals(spatialEntityId.EntityId))
                        {

                            if (HelperFunctions.Intersect(potentialCollision.position - otherBoxCollider.Position, otherBoxCollider.Dimensions,
                                entityTransform.Position - boxCollider.Position, boxCollider.Dimensions))
                            {
                                newCollisions[potentialCollision.entityId] = new CollisionSchema.CollisionPoint
                                {
                                    CollidingWith = potentialCollision.entityId,
                                    Distance = potentialCollision.position - entityTransform.Position
                                };
                            }
                            else
                            {
                                // Raise on leave event if was in previous collisions
                            }
                        }


                        if (newCollisions.Count > 0)
                        {
                            componentUpdateSystem.SendEvent(new CollisionSchema.Collision.OnCollision.Event(new CollisionSchema.CollisionEventPayload
                            {
                                CollidedWith = newCollisions
                            }), spatialEntityId.EntityId);
                        }
                        collisionComponent.Collisions = newCollisions;
                    }
                }
            });

            /* My approach to this wasn't the best, should've stayed using the AABB tree.
             * It is what it is.
            #region Broadphase, getting potential collisions

            NativeHashMap<EntityId, int> entityIdToRegionIndex = new NativeHashMap<EntityId, int>(updateCollisionGroup.CalculateEntityCount(), Allocator.TempJob);

            // region index to colliders within that region.
            // Nested native containers not possible.
            NativeHashMap<int, NativeList<ColliderCheck>> regionIndexToColliders = new NativeHashMap<int, NativeList<ColliderCheck>>(positionSystem.GetRegionCount(), Allocator.TempJob);

            /*

            // This rigt here is disgusting there HAS to be a better way. I'm essentially taking from spatial partition.
            // then partitioning it further but with storing colliders.
            // might as well just store colliders directly in quad nodes.
            // I'll still need to do loop. One solution is to make the spatialPartioning structure globally accessible and static.
            // They must access it from position system. It initself cannot be singleton as will reuse the structure, but hmm.
            // maybe through decorator pattern over quad tree it can be single ton.
            // but yeah, this block of disgustingness is temporary.
            Entities.With(checkCollisionGroup).ForEach((ref SpatialEntityId spatialEntityId, ref EntityTransform.Component entityTransform, ref CollisionSchema.BoxCollider.Component collider) =>
            {
                bool mappedPrepulledRegion = false;

                for (int regionIndex = 0; regionIndex < regionsChecked.Count; ++regionIndex)
                {
                    QuadNode regionChecking = regionsChecked[regionIndex];
                    // Then reuse this region.
                    if (HelperFunctions.IsWithinRegion(regionChecking.center, regionChecking.dimensions, entityTransform.Position))
                    {
                        mappedPrepulledRegion = true;
                        regionIndexToColliders[regionIndex].Add(new ColliderCheck
                        {
                            entityId = spatialEntityId.EntityId,
                            center = collider.Position,
                            dimensions = collider.Dimensions
                        });
                    }
                }
                // Otherwise query the quad tree.
                if (!mappedPrepulledRegion)
                {
                    List<QuadNode> quadNodes = positionSystem.querySpatialPartition(entityTransform.Position);
                    regionsChecked.Add(quadNodes[0]);
                    regionIndexToColliders[regionsChecked.Count - 1] = new NativeList<ColliderCheck>(quadNodes.Count, Allocator.TempJob);
                    // Could add all in quad notes to this, then let above loop just not add anything. But would mean
                    // fetching again when this outer loop has already fetched the data. This is better approach.
                    regionIndexToColliders[regionsChecked.Count - 1].Add(new ColliderCheck
                    {
                        entityId = spatialEntityId.EntityId,
                        center = collider.Position,
                        dimensions = collider.Dimensions
                    });

                }
            });
            #endregion


            #region Narrow phase
            NativeHashMap<EntityId, NativeList<CollisionSchema.CollisionPoint>> entityIdToCollisionPoints = new NativeHashMap<EntityId, NativeList<CollisionSchema.CollisionPoint>>(entityIdToRegionIndex.Length, Allocator.TempJob);
            CheckCollisionJob checkCollisionJob = new CheckCollisionJob
            {
                entityIdToRegionIndex = entityIdToRegionIndex,
                regionIndexToColliders = regionIndexToColliders,
                entityIdToCollisions = entityIdToCollisionPoints.AsParallelWriter()
            };
            checkCollisionJob.Schedule(checkCollisionGroup).Complete();
            entityIdToRegionIndex.Dispose();
            regionIndexToColliders.Dispose();
            #endregion

            #region Update Collision Components
            Entities.With(updateCollisionGroup).ForEach((ref SpatialEntityId spatialEntityId, ref EntityTransform.Component entityTransform, ref CollisionSchema.Collision.Component collisionComponent) =>
            {
                if (entityIdToCollisionPoints.TryGetValue(spatialEntityId.EntityId, out NativeList<CollisionSchema.CollisionPoint> collisionsToAdd))
                {
                    Dictionary<EntityId, CollisionSchema.CollisionPoint> currentCollisions = collisionComponent.Collisions;

                    foreach (CollisionSchema.CollisionPoint collisionPoint in collisionsToAdd)
                    {
                        currentCollisions[collisionPoint.CollidingWith] = collisionPoint;
                    }

                    collisionComponent.Collisions = currentCollisions;
                    collisionsToAdd.Dispose();

                }
            });

            entityIdToCollisionPoints.Dispose();
            #endregion
            
             */

        }
    }
}