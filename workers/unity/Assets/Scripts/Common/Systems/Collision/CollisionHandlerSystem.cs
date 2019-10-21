using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using CollisionSchema = MdgSchema.Common.Collision;
using PositionSchema = MdgSchema.Common.Position;
using Improbable;
using MdgSchema.Common;

namespace MDG.Common.Systems.Collision {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(CollisionDetectionSystem))]
    public class CollisionHandlerSystem : JobComponentSystem
    {
        EntityQuery authoritativeVelocityGroup;
        EntityQuery authPosGroup;
        NativeHashMap<EntityId, bool> toReverse;
        JobHandle undoPositionJobHandle;
        ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            authPosGroup = GetEntityQuery(
             ComponentType.ReadOnly<SpatialEntityId>(),
             ComponentType.ReadOnly<PositionSchema.LinearVelocity.Component>(),
             ComponentType.ReadOnly<PositionSchema.AngularVelocity.Component>(),
             ComponentType.ReadWrite<EntityTransform.Component>(),
             ComponentType.ReadOnly<EntityTransform.ComponentAuthority>()
             );
            authPosGroup.SetFilter(EntityTransform.ComponentAuthority.Authoritative);
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        struct GetCollisionsJob : IJobForEach<SpatialEntityId, CollisionSchema.Collision.Component, CollisionSchema.BoxCollider.Component>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, bool>.ParallelWriter toReverse;
            public void Execute([ReadOnly] ref SpatialEntityId c0, [ReadOnly] ref CollisionSchema.Collision.Component c1, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider)
            {
                // Add check here for if collider is trigger.
                // or make it it's own thing? Or add to query, physical component or something.
                if (!boxCollider.IsTrigger && c1.Collisions.Count > 0)
                {
                    toReverse.TryAdd(c0.EntityId, true);
                }
            }
        }

        struct UndoPositionChangeJob : IJobForEach<SpatialEntityId, PositionSchema.LinearVelocity.Component, EntityTransform.Component>
        {
            public float deltaTime;
            [ReadOnly]
            public NativeHashMap<EntityId, bool> toReverse;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref PositionSchema.LinearVelocity.Component velocityComponent,
                ref EntityTransform.Component entityTransformComponent)
            {
                if (toReverse.TryGetValue(spatialEntityId.EntityId, out bool _))
                {
                    Debug.Log("Reversing applied velocity");
                    entityTransformComponent.Position -= (velocityComponent.Velocity * deltaTime) * 2;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float deltaTime = Time.deltaTime;

            if (toReverse.IsCreated)
            {
            }
            toReverse = new NativeHashMap<EntityId, bool>(authPosGroup.CalculateEntityCount(), Allocator.TempJob);
            GetCollisionsJob getCollisionsJob = new GetCollisionsJob
            {
                toReverse = toReverse.AsParallelWriter()
            };

            inputDeps = getCollisionsJob.Schedule(this, inputDeps);

            UndoPositionChangeJob undoPositionChangeJob = new UndoPositionChangeJob
            {
                deltaTime = deltaTime,
                toReverse = toReverse
            };
            inputDeps.Complete();
            undoPositionJobHandle = undoPositionChangeJob.Schedule(authPosGroup, inputDeps);
            undoPositionJobHandle.Complete();
            toReverse.Dispose();


            /* Keeping here since not good use case not thinking rigt for this
             * but perfect for soemthinf else in mind.
            // Recieve events, then run jobs.
            var eventsRecieved = componentUpdateSystem.GetEventsReceived<CollisionSchema.Collision.OnCollision.Event>();

            Queue<EntityId> idQueue = new Queue<EntityId>();
            Queue<JobHandle> velocityUpdates = new Queue<JobHandle>();
            LinkedList<NativeArray<Vector3f>> velocities = new LinkedList<NativeArray<Vector3f>>();
            NativeHashMap<EntityId, Vector3f> entityIdToVelocity = new NativeHashMap<EntityId, Vector3f>(eventsRecieved.Count, Allocator.TempJob);
            LinkedList<NativeList<CollisionSchema.CollisionPoint>> collisionPointsToDispose = new LinkedList<NativeList<CollisionSchema.CollisionPoint>>();

            for (int i = 0; i < eventsRecieved.Count; i += 1)
            {
                ref readonly var eventRecieved = ref eventsRecieved[i];
          
                NativeList<CollisionSchema.CollisionPoint> collisionPoints = new NativeList<CollisionSchema.CollisionPoint>(eventRecieved.Event.Payload.CollidedWith.Count, Allocator.TempJob);

                foreach(KeyValuePair<EntityId, CollisionSchema.CollisionPoint> keyValuePair in eventRecieved.Event.Payload.CollidedWith)
                {
                    collisionPoints.Add(keyValuePair.Value);
                }
                collisionPointsToDispose.AddLast(collisionPoints);

                idQueue.Enqueue(eventRecieved.EntityId);
                velocities.AddLast(new NativeArray<Vector3f>(1, Allocator.TempJob));

                CalculateLinearVelocityBasedOnCollisionJob calculateLinearVelocityBasedOnCollisionJob = new CalculateLinearVelocityBasedOnCollisionJob
                {
                    collisions = collisionPointsToDispose.Last.Value,
                    linearVelocity = velocities.Last.Value
                };
                velocityUpdates.Enqueue(calculateLinearVelocityBasedOnCollisionJob.Schedule(collisionPoints.Length, 64));
            }

            while (velocityUpdates.Count > 0)
            {
                JobHandle velocityUpdateJobHandle = velocityUpdates.Dequeue();
                velocityUpdateJobHandle.Complete();
                collisionPointsToDispose.First.Value.Dispose();
                collisionPointsToDispose.RemoveFirst();
                EntityId nextId = idQueue.Dequeue();
                var updatedVelocity = velocities.First.Value;
                velocities.RemoveFirst();
                entityIdToVelocity.TryAdd(nextId, updatedVelocity[0]);
                updatedVelocity.Dispose();
            }

            ApplyNewVelocity applyNewVelocityJob = new ApplyNewVelocity
            {
                entityIdToVelocity = entityIdToVelocity
            };
            applyNewVelocityJob.Schedule(authoritativeVelocityGroup).Complete();
            entityIdToVelocity.Dispose();
            */
            return undoPositionJobHandle;
        }
    }
}