using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using PositionSchema = MdgSchema.Common.Position;
using CollisionSchema = MdgSchema.Common.Collision;
using Improbable;
using MDG.Invader.Components;
using MdgSchema.Common;

namespace MDG.Invader.Systems
{
    /// <summary>
    /// Checks for collision events on units.
    /// Reroute them by altering velocity for the moment or appling angular velocity.
    /// Angular velocity is in itself applied to velocity. Could do same way did ocean travel, should look into alternatives.
    /// I have linear velocity, I'll derive orientation from that, Then update velocity accordingl. I'd have to map out alternate path to same destination.
    /// // Incase I go with this, how should I implement.
    /// </summary>
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class UnitRerouteSystem : ComponentSystem
    {
        ComponentUpdateSystem componentUpdateSystem;
        WorkerSystem workerSystem;
        EntityQuery rerouteGroup;
        struct ScheduleRedirectJobInfo
        {
            public EntityId entityId;
            public JobHandle jobHandle;
        }

        int framesSinceUpdate = 0;
        int frameBuffer = 1;


        // Apply velocity towards actual destination over rerouted velocity.
        struct ResolveRerouteJob : IJobForEachWithEntity<RerouteComponent, PositionSchema.LinearVelocity.Component, EntityTransform.Component>
        {
            public float deltaTime;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            public void Execute(Entity entity, int index, ref RerouteComponent rerouteComponent, ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
                [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                if (!rerouteComponent.applied)
                {
                    linearVelocityComponent.Velocity = rerouteComponent.subDestination;
                    rerouteComponent.applied = true;
                }
                else
                {
                    Vector3f velocityTowardsDestination = rerouteComponent.destination - entityTransform.Position;
                    linearVelocityComponent.Velocity += velocityTowardsDestination * deltaTime;

                    float dotProduct = Vector3.Dot(linearVelocityComponent.Velocity.ToUnityVector().normalized,
                        velocityTowardsDestination.ToUnityVector().normalized);
                    // Then it's velocity is targeting destination likely.
                    if (dotProduct > 0.8f)
                    {
                        Debug.Log("Stopping reroute");
                        linearVelocityComponent.Velocity = velocityTowardsDestination;
                        entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                    }
                }
            }
        }


        struct TryRerouteJob : IJobParallelFor
        {
            [ReadOnly]
            public Vector3f currentVelocity;

            [WriteOnly]
            public NativeQueue<Vector3f>.ParallelWriter potentialRoutes;

            [ReadOnly]
            public NativeArray<CollisionSchema.CollisionPoint> collisionPoints;

            public void Execute(int index)
            {
                Vector3 collisionPointDistNormal = collisionPoints[index].Distance.ToUnityVector().normalized;
                float magnitude = currentVelocity.ToUnityVector().magnitude;
                float initialAngle = Mathf.Atan2(currentVelocity.Z, currentVelocity.X);
                float deltaAngle = initialAngle;
                float totalAngleIncrement = 0;
                do
                {
                    totalAngleIncrement += 1.0f;
                    Vector3f vectorToTryNormalized = new Vector3f(Mathf.Cos(initialAngle + totalAngleIncrement), 0, 
                        Mathf.Sin(initialAngle + totalAngleIncrement));
                    Debug.Log("Vector positive radian direction " + vectorToTryNormalized);
                    // Check dot product to see if still tends to direction of this collision.
                    float dotProduct = Vector3.Dot(collisionPointDistNormal, vectorToTryNormalized.ToUnityVector());
                   
                    // It works, but it's trying routes that will fail since only take into account
                    // point not size of colliders in reroute
                    if (dotProduct < 0)
                    {
                        potentialRoutes.Enqueue(vectorToTryNormalized * magnitude);
                        break;
                    }
                    vectorToTryNormalized.X = Mathf.Cos(initialAngle - totalAngleIncrement);
                    vectorToTryNormalized.Z = Mathf.Sin(initialAngle - totalAngleIncrement);

                    Debug.Log("Vector negative radian direction " + vectorToTryNormalized);

                    dotProduct = Vector3.Dot(collisionPointDistNormal, vectorToTryNormalized.ToUnityVector());

                    if (dotProduct < 0)
                    {
                        potentialRoutes.Enqueue(vectorToTryNormalized * magnitude);
                        break;
                    }

                } while (totalAngleIncrement < 360);
            }
        }


        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("I happen tho right?");
            rerouteGroup = GetEntityQuery(
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadWrite<RerouteComponent>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>()
                );

            rerouteGroup.SetFilter(PositionSchema.LinearVelocity.ComponentAuthority.Authoritative);

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            ResolveRerouteJob resolveRerouteJob = new ResolveRerouteJob
            {
                deltaTime = deltaTime,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };

            JobHandle scheduledReroute = resolveRerouteJob.Schedule(rerouteGroup);
            scheduledReroute.Complete();

            framesSinceUpdate += 1;
            if (framesSinceUpdate <= frameBuffer)
            {
                Debug.Log("skipping");
               // return;
            }
            framesSinceUpdate = 0;
            Debug.Log("Unit reroute system happening");

            #region Processing Collision Events
            var events = componentUpdateSystem.GetEventsReceived<CollisionSchema.Collision.OnCollision.Event>();
            Queue<ScheduleRedirectJobInfo> scheduledJobs = new Queue<ScheduleRedirectJobInfo>();
            Dictionary<EntityId, NativeQueue<Vector3f>> entityIdToPotentialRedirects = new Dictionary<EntityId, NativeQueue<Vector3f>>();
            LinkedList<NativeArray<CollisionSchema.CollisionPoint>> toDispose = new LinkedList<NativeArray<CollisionSchema.CollisionPoint>>();

            for (int i = 0; i < events.Count; ++i)
            {
                Debug.Log("Got events");
                ref readonly var eventSent = ref events[i];

                Dictionary<EntityId, CollisionSchema.CollisionPoint> collidedWith = eventSent.Event.Payload.CollidedWith;
                NativeArray<CollisionSchema.CollisionPoint> collisionPoints = new NativeArray<CollisionSchema.CollisionPoint>(collidedWith.Count, Allocator.TempJob);
                int pointsAdded = 0;
                entityIdToPotentialRedirects[eventSent.EntityId] = new NativeQueue<Vector3f>(Allocator.TempJob);
                // Perhaps some units have more specialized behaviour, will add as component later as need be.
                // but this is basic behaviour needed for AI. Also No auto rerouting against traps unless notice them.
                // colliding with those is interesting.
                foreach (KeyValuePair<EntityId, CollisionSchema.CollisionPoint> keyValuePair in collidedWith)
                {
                    collisionPoints[pointsAdded++] = keyValuePair.Value;
                }

                workerSystem.TryGetEntity(eventSent.EntityId, out Entity entity);
                PositionSchema.LinearVelocity.Component linearVelocityComponent = EntityManager.GetComponentData<PositionSchema.LinearVelocity.Component>(entity);
                TryRerouteJob tryRerouteJob = new TryRerouteJob
                {
                    collisionPoints = collisionPoints,
                    currentVelocity = linearVelocityComponent.Velocity,
                    potentialRoutes = entityIdToPotentialRedirects[eventSent.EntityId].AsParallelWriter()
                };
                toDispose.AddLast(collisionPoints);

                ScheduleRedirectJobInfo scheduleRedirectJob = new ScheduleRedirectJobInfo
                {
                    entityId = eventSent.EntityId,
                    jobHandle = tryRerouteJob.Schedule(collisionPoints.Length, 32)
                };
                scheduledJobs.Enqueue(scheduleRedirectJob);
            }

            while (scheduledJobs.Count > 0)
            {
                ScheduleRedirectJobInfo scheduleRedirectJob = scheduledJobs.Dequeue();
                scheduleRedirectJob.jobHandle.Complete();

                // Right now thinking in terms of applying reroute as velocity, when in reality I want it sub destination.
                NativeQueue<Vector3f> potentialReroutes = entityIdToPotentialRedirects[scheduleRedirectJob.entityId];
                entityIdToPotentialRedirects.Remove(scheduleRedirectJob.entityId);

                NativeArray<CollisionSchema.CollisionPoint> collisionPoints = toDispose.First.Value;
                toDispose.RemoveFirst();

                workerSystem.TryGetEntity(scheduleRedirectJob.entityId, out Entity entity);

                // May also jobify this down line. But this does give time for rest of scheduled jobs to complete.
                Vector3f? rerouteVector = null;
                while (potentialReroutes.Count > 0)
                {
                    Vector3f potentialRoute = potentialReroutes.Dequeue();

                    bool goodAgainstAll = true;
                    for (int i = 0; i < collisionPoints.Length; ++i)
                    {
                        float dotProduct = Vector3.Dot(
                            potentialRoute.ToUnityVector().normalized, 
                            collisionPoints[i].Distance.ToUnityVector().normalized);
                        if (dotProduct > 0.3f)
                        {
                            // If meets this, check bounds, so essentially
                            // see if this vector is within collider of collision poinst.
                            CollisionSchema.BoxCollider.Component boxCollider = EntityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(entity);
                           
                            // So compare potential route to see if ends up being inside boxCollider
                            // of this collision. For now, final result ends up correct, so not worry about.

                            goodAgainstAll = false;
                            break;
                        }
                    }
                    if (goodAgainstAll)
                    {
                        rerouteVector = potentialRoute;
                        break;
                    }
                    // Has to be within dot threshhold for all collision points to be valid.
                    // This is also not even fucking tkaing into account box collider dimensions.
                    // but that will be later, it's just adding padding. same logic.    
                }
                potentialReroutes.Dispose();
                collisionPoints.Dispose();
                // If there was a valid redirect, add the redirect component, otherwise Unit cannot move.
                // This will be non spatial component
                if (rerouteVector.HasValue)
                {
                    PositionSchema.LinearVelocity.Component linearVelocityComponent = EntityManager.GetComponentData<PositionSchema.LinearVelocity.Component>(entity);
                    EntityTransform.Component entityTransform = EntityManager.GetComponentData<EntityTransform.Component>(entity);

                    RerouteComponent rerouteComponent = new RerouteComponent
                    {
                        destination = linearVelocityComponent.Velocity + entityTransform.Position,
                        subDestination = rerouteVector.Value,
                        applied = false
                    };

                    // Set destination as velocity plus current position to get destination
                    if (!EntityManager.HasComponent<RerouteComponent>(entity))
                    {
                        PostUpdateCommands.AddComponent(entity, rerouteComponent);
                    }
                    else
                    {
                        EntityManager.SetComponentData(entity, rerouteComponent);
                    }
                }
            }
            #endregion
            
        }
    }
}