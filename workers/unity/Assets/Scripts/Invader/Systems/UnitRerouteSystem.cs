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
using MDG.Common;

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
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(CommandUpdateSystem))]
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
        int frameBuffer = 5;


        // Apply velocity towards actual destination over rerouted velocity.
        // Auto cancel reroute if no command given.
        struct ResolveRerouteJob : IJobForEachWithEntity<RerouteComponent, PositionSchema.LinearVelocity.Component, EntityTransform.Component,
            CollisionSchema.BoxCollider.Component, CommandListener>
        {
            public float deltaTime;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            public void Execute(Entity entity, int index, ref RerouteComponent rerouteComponent, ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
                [ReadOnly] ref EntityTransform.Component entityTransform, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                [ReadOnly] ref CommandListener commandListener)
            {

                if (commandListener.CommandType == Commands.CommandType.None)
                {
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                    linearVelocityComponent.Velocity = Vector3f.Zero;
                }
                else
                {
                    if (!rerouteComponent.applied)
                    {
                        // Add.
                        linearVelocityComponent.Velocity = rerouteComponent.subDestination;
                        rerouteComponent.applied = true;
                    }
                    else
                    {
                        Vector3f velocityTowardsDestination = rerouteComponent.destination - entityTransform.Position;
                        linearVelocityComponent.Velocity = velocityTowardsDestination;
                        float distance = HelperFunctions.Distance(rerouteComponent.destination, entityTransform.Position);

                        if (distance < boxCollider.Dimensions.ToUnityVector().magnitude)
                        {
                            Debug.Log("Finished moving");
                            linearVelocityComponent.Velocity = Vector3f.Zero;
                            entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                        }

                        /*
                        // I need let reroute just happen.
                        Vector3f velocityTowardsDestination = rerouteComponent.destination - entityTransform.Position;
                        linearVelocityComponent.Velocity += velocityTowardsDestination * deltaTime;

                        float dotProduct = Vector3.Dot(linearVelocityComponent.Velocity.ToUnityVector().normalized,
                            velocityTowardsDestination.ToUnityVector().normalized);
                        // Then it's velocity is targeting destination likely.
                        if (dotProduct > 0.5f)
                        {
                            Debug.Log("Stopping reroute");
                            linearVelocityComponent.Velocity = velocityTowardsDestination;
                            entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                        }*/
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
                Vector3 collisionPointDistNormalized = collisionPoints[index].Distance.ToUnityVector().normalized;
                Vector3 convertedVelocity = currentVelocity.ToUnityVector().normalized;
                float velocityMagnitude = currentVelocity.ToUnityVector().magnitude;
                float initialAngle = Vector3.Angle(convertedVelocity, collisionPointDistNormalized);
                int incrementDirection = HelperFunctions.IsLeftOfVector(collisionPointDistNormalized, convertedVelocity) ? 1 : -1;
                float totalAngleIncrement = 0;
                int max = 180 * incrementDirection;
                do
                {
                    totalAngleIncrement += incrementDirection;
                    float newAngle = initialAngle + totalAngleIncrement;

                    Vector3f newVelocity = new Vector3f(Mathf.Cos(newAngle), 0, Mathf.Sin(newAngle));
                    // Check dot product to see if still tends to direction of this collision.
                    float dotProduct = Vector3.Dot(collisionPointDistNormalized, newVelocity.ToUnityVector());
                    // It works, but it's trying routes that will fail since only take into account
                    // point not size of colliders in reroute
                    if (dotProduct < 0.8f)
                    {
                        potentialRoutes.Enqueue(newVelocity * velocityMagnitude);
                        break;
                    }
                } while (totalAngleIncrement < max);
            }
        }


        protected override void OnCreate()
        {
            base.OnCreate();
            rerouteGroup = GetEntityQuery(
                ComponentType.ReadOnly<EntityTransform.Component>(),
                ComponentType.ReadWrite<RerouteComponent>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                ComponentType.ReadOnly<CommandListener>()
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
             //   return;
            }
            framesSinceUpdate = 0;

            #region Processing Collision Events
            var events = componentUpdateSystem.GetEventsReceived<CollisionSchema.Collision.OnCollision.Event>();
            Queue<ScheduleRedirectJobInfo> scheduledJobs = new Queue<ScheduleRedirectJobInfo>();
            Dictionary<EntityId, NativeQueue<Vector3f>> entityIdToPotentialRedirects = new Dictionary<EntityId, NativeQueue<Vector3f>>();
            Dictionary<EntityId, NativeQueue<Vector3f>> allPotentialReroutes = new Dictionary<EntityId, NativeQueue<Vector3f>>();
            LinkedList<NativeArray<CollisionSchema.CollisionPoint>> toDispose = new LinkedList<NativeArray<CollisionSchema.CollisionPoint>>();

            for (int i = 0; i < events.Count; ++i)
            {
                ref readonly var eventSent = ref events[i];
                workerSystem.TryGetEntity(eventSent.EntityId, out Entity entity);
                if (!EntityManager.HasComponent<CommandListener>(entity) || EntityManager.GetComponentData<CommandListener>(entity).CommandType == Commands.CommandType.None)
                {
                    continue;
                }

                if (allPotentialReroutes.ContainsKey(eventSent.EntityId))
                {
                    continue;
                }

                Dictionary<EntityId, CollisionSchema.CollisionPoint> collidedWith = eventSent.Event.Payload.CollidedWith;
                NativeArray<CollisionSchema.CollisionPoint> collisionPoints = new NativeArray<CollisionSchema.CollisionPoint>(collidedWith.Count, Allocator.TempJob);
                int pointsAdded = 0;
                // Could make it a queue instead.
                NativeQueue<Vector3f> potentialReroutes = new NativeQueue<Vector3f>(Allocator.TempJob);
                // Perhaps some units have more specialized behaviour, will add as component later as need be.
                // but this is basic behaviour needed for AI. Also No auto rerouting against traps unless notice them.
                // colliding with those is interesting.
                foreach (KeyValuePair<EntityId, CollisionSchema.CollisionPoint> keyValuePair in collidedWith)
                {
                    collisionPoints[pointsAdded++] = keyValuePair.Value;
                }

               
                PositionSchema.LinearVelocity.Component linearVelocityComponent = EntityManager.GetComponentData<PositionSchema.LinearVelocity.Component>(entity);
                TryRerouteJob tryRerouteJob = new TryRerouteJob
                {
                    collisionPoints = collisionPoints,
                    currentVelocity = linearVelocityComponent.Velocity,
                    potentialRoutes = potentialReroutes.AsParallelWriter()
                };
                toDispose.AddLast(collisionPoints);

                ScheduleRedirectJobInfo scheduleRedirectJob = new ScheduleRedirectJobInfo
                {
                    entityId = eventSent.EntityId,
                    jobHandle = tryRerouteJob.Schedule(collisionPoints.Length, 1)
                };
                allPotentialReroutes[eventSent.EntityId] = (potentialReroutes);
                scheduledJobs.Enqueue(scheduleRedirectJob);
            }

            while (scheduledJobs.Count > 0)
            {
                ScheduleRedirectJobInfo scheduleRedirectJob = scheduledJobs.Peek();

                scheduleRedirectJob.jobHandle.Complete();
                // Right now thinking in terms of applying reroute as velocity, when in reality I want it sub destination.
                NativeQueue<Vector3f> potentialReroutes = allPotentialReroutes[scheduleRedirectJob.entityId];

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

                scheduledJobs.Dequeue();
                allPotentialReroutes.Remove(scheduleRedirectJob.entityId);
                // If there was a valid redirect, add the redirect component, otherwise Unit cannot move.
                // This will be non spatial component
                if (rerouteVector.HasValue && rerouteVector.Value != Vector3f.Zero)
                {
                    PositionSchema.LinearVelocity.Component linearVelocityComponent = EntityManager.GetComponentData<PositionSchema.LinearVelocity.Component>(entity);
                    EntityTransform.Component entityTransform = EntityManager.GetComponentData<EntityTransform.Component>(entity);

                    CommandListener commandListener = EntityManager.GetComponentData<CommandListener>(entity);
                    RerouteComponent rerouteComponent = new RerouteComponent
                    {
                        destination = commandListener.TargetPosition,
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