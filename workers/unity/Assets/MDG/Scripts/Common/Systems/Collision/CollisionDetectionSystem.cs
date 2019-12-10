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
using MdgSchema.Common.Util;

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
                    ComponentType.ReadOnly<EntityPosition.Component>(),
                    ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>()
                );
        }

        // If didn't jobify it. Maybe could jobify subloop of it??
        private struct CheckCollisionJob : IJobForEach<SpatialEntityId, EntityPosition.Component, CollisionSchema.BoxCollider.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> entityIdToRegionIndex;
            [ReadOnly]
            public NativeHashMap<int, NativeList<ColliderCheck>> regionIndexToColliders;

            [WriteOnly]
            public NativeHashMap<EntityId, NativeList<CollisionSchema.CollisionPoint>>.ParallelWriter entityIdToCollisions;

            // Need to add queue of events need to send for each collision.
            // if still wanna do that. That could be resolved frame after this completes though.
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref EntityPosition.Component EntityPosition, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider)
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
                                HelperFunctions.Subtract(EntityPosition.Position,boxCollider.Position), boxCollider.Dimensions))
                            {
                                collisionPoints.Add(new CollisionSchema.CollisionPoint
                                {
                                    CollidingWith = colliderCheck.entityId,
                                    Distance = HelperFunctions.Subtract(EntityPosition.Position,colliderCheck.center)
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
            // Quick collision detection, zero optimization apart from continous broadphase
            // Scrapping to use unity's built in collision. This was wasted effort should've let them handle detection and just sned requests
            // to update collision accordingly. Had hoped to reduce about computation done
            // but switching to unity now will make it more scalable for expansion.     
            Entities.With(updateCollisionGroup).ForEach((ref SpatialEntityId spatialEntityId, ref EntityPosition.Component EntityPosition, 
                ref CollisionSchema.BoxCollider.Component boxCollider, ref CollisionSchema.Collision.Component collisionComponent) =>
            {
                Dictionary<EntityId, CollisionSchema.CollisionPoint> previousCollisions = collisionComponent.Collisions;
                Dictionary<EntityId, CollisionSchema.CollisionPoint> newCollisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();

                Dictionary<EntityId, CollisionSchema.CollisionPoint> prevTriggers = collisionComponent.Triggers;
                Dictionary<EntityId, CollisionSchema.CollisionPoint> newTriggers = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
                List<QuadNode> potentialCollisions = positionSystem.querySpatialPartition(EntityPosition.Position);

                //Theoritically could jobify this loop I think.
                foreach (QuadNode potentialCollision in potentialCollisions)
                { 
                    if (workerSystem.TryGetEntity(potentialCollision.entityId, out Entity entity) && EntityManager.HasComponent<CollisionSchema.BoxCollider.Component>(entity))
                    {
                        CollisionSchema.BoxCollider.Component otherBoxCollider = EntityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(entity);
                        if (!potentialCollision.entityId.Equals(spatialEntityId.EntityId))
                        {

                            Vector3f offSetOther = HelperFunctions.Subtract(potentialCollision.position, otherBoxCollider.Position);
                            Vector3f offSet = HelperFunctions.Subtract(EntityPosition.Position, boxCollider.Position);
                            if (HelperFunctions.Intersect(offSetOther, otherBoxCollider.Dimensions,
                                offSet, boxCollider.Dimensions))
                            {
                                // If other I collided with was trigger, it gets added to my triggers list.
                                // else for example if I am trigger, non triggers goes in my colliders list.
                                if (otherBoxCollider.IsTrigger)
                                {
                                    newTriggers[potentialCollision.entityId] = new CollisionSchema.CollisionPoint
                                    {
                                        CollidingWith = potentialCollision.entityId,
                                        Distance = HelperFunctions.Subtract(potentialCollision.position, EntityPosition.Position)
                                    };
                                }
                                else
                                {
                                    newCollisions[potentialCollision.entityId] = new CollisionSchema.CollisionPoint
                                    {
                                        CollidingWith = potentialCollision.entityId,
                                        Distance = HelperFunctions.Subtract(potentialCollision.position, EntityPosition.Position)
                                    };
                                }
                            }
                            else
                            {
                                // Raise on leave event if was in previous collisions
                            }
                        }

                        if (newCollisions.Count > 0)
                        {
                            componentUpdateSystem.SendEvent(new CollisionSchema.Collision.CollisionHappen.Event(new CollisionSchema.CollisionEventPayload
                            {
                                CollidedWith = newCollisions
                            }), spatialEntityId.EntityId);
                        }
                        if (newTriggers.Count > 0)
                        {
                            componentUpdateSystem.SendEvent(new CollisionSchema.Collision.TriggerHappen.Event(new CollisionSchema.CollisionEventPayload
                            {
                                CollidedWith = newTriggers
                            }), spatialEntityId.EntityId);
                        }
                        collisionComponent.Triggers = newTriggers;
                        collisionComponent.Collisions = newCollisions;
                    }
                }
            });

        }
    }
}