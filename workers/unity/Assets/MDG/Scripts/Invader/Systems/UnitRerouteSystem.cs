using Improbable.Gdk.Core;
using MDG.Common;
using MDG.Invader.Components;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using CollisionSchema = MdgSchema.Common.Collision;
using PositionSchema = MdgSchema.Common.Position;

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
        struct ResolveRerouteJob : IJobForEachWithEntity<RerouteComponent, PositionSchema.LinearVelocity.Component, EntityPosition.Component,
            CollisionSchema.BoxCollider.Component, CommandListener>
        {
            public float deltaTime;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            public void Execute(Entity entity, int index, ref RerouteComponent rerouteComponent, ref PositionSchema.LinearVelocity.Component linearVelocityComponent, 
                [ReadOnly] ref EntityPosition.Component EntityPosition, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                [ReadOnly] ref CommandListener commandListener)
            {

                if (commandListener.CommandType == CommandType.None || HelperFunctions.IsEqual(linearVelocityComponent.Velocity, new Vector3f(0,0,0)))
                {
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                    linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                }
                else
                {
                    if (!rerouteComponent.Applied)
                    {
                        linearVelocityComponent.Velocity = rerouteComponent.SubDestination;
                        rerouteComponent.Applied = true;
                    }
                    else
                    {
                        // I don't want to do this, cause will constantly reroute.
                        Vector3f velocityTowardsDestination = HelperFunctions.Subtract(rerouteComponent.Destination, EntityPosition.Position);
                        linearVelocityComponent.Velocity = velocityTowardsDestination;

                        Vector3 dest = HelperFunctions.Vector3fToVector3(rerouteComponent.Destination);
                        Vector3 pos = HelperFunctions.Vector3fToVector3(EntityPosition.Position);
                        Vector3 dimensions = HelperFunctions.Vector3fToVector3(boxCollider.Dimensions);
                        float distance = Vector3.Distance(dest, pos);

                        if (distance < dimensions.magnitude)
                        {
                            Debug.Log("Finished moving");
                            linearVelocityComponent.Velocity = new Vector3f(0, 0, 0);
                            entityCommandBuffer.RemoveComponent(index, entity, typeof(RerouteComponent));
                        }

                        /*
                        // I need let reroute just happen.
                        Vector3f velocityTowardsDestination = HelperFunctions.Subtract(rerouteComponent.destination,EntityPosition.Position);
                        velocityTowardsDestination = HelperFunctions.Scale(velocityTowardsDestination,deltaTime);

                        Vector3 convertedDestinationVelocity = HelperFunctions.Vector3fToVector3(HelperFunctions.Normalize(velocityTowardsDestination));
                        Vector3 convertedVelocity = HelperFunctions.Vector3fToVector3(HelperFunctions.Normalize(linearVelocityComponent.Velocity));
                        float dotProduct = Vector3.Dot(convertedDestinationVelocity, convertedVelocity);
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
            public NativeQueue<Vector3>.ParallelWriter potentialRoutes;

            [ReadOnly]
            public NativeArray<CollisionSchema.CollisionPoint> collisionPoints;

            public void Execute(int index)
            {
                Vector3 collisionPointDistNormalized = HelperFunctions.Vector3fToVector3(collisionPoints[index].Distance).normalized;
                Vector3 convertedVelocity = HelperFunctions.Vector3fToVector3(currentVelocity);
                float velocityMagnitude = convertedVelocity.magnitude;
                convertedVelocity = convertedVelocity.normalized;
                float initialAngle = Vector3.Angle(convertedVelocity, collisionPointDistNormalized);
                int incrementDirection = HelperFunctions.IsLeftOfVector(collisionPointDistNormalized, convertedVelocity) ? 1 : -1;
                float totalAngleIncrement = 0;
                int max = 180 * incrementDirection;
                do
                {
                    totalAngleIncrement += incrementDirection;
                    float newAngle = initialAngle + totalAngleIncrement;
                    Vector3 newVelocity = new Vector3(Mathf.Cos(newAngle), 0, Mathf.Sin(newAngle));
                    // Check dot product to see if still tends to direction of this collision.
                    float dotProduct = Vector3.Dot(collisionPointDistNormalized, newVelocity);
                    // It works, but it's trying routes that will fail since only take into account
                    // point not size of colliders in reroute
                    if (dotProduct < 0.5f)
                    {
                        Vector3 potentialReroute = newVelocity * velocityMagnitude;
                        potentialReroute.y = currentVelocity.Y;
                        potentialRoutes.Enqueue(potentialReroute);
                        break;
                    }
                } while (totalAngleIncrement < max);
            }
        }


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            rerouteGroup = GetEntityQuery(
              ComponentType.ReadOnly<EntityPosition.Component>(),
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
            var events = componentUpdateSystem.GetEventsReceived<CollisionSchema.Collision.CollisionHappen.Event>();
            Queue<ScheduleRedirectJobInfo> scheduledJobs = new Queue<ScheduleRedirectJobInfo>();
            Dictionary<EntityId, NativeQueue<Vector3>> entityIdToPotentialRedirects = new Dictionary<EntityId, NativeQueue<Vector3>>();
            Dictionary<EntityId, NativeQueue<Vector3>> allPotentialReroutes = new Dictionary<EntityId, NativeQueue<Vector3>>();
            LinkedList<NativeArray<CollisionSchema.CollisionPoint>> toDispose = new LinkedList<NativeArray<CollisionSchema.CollisionPoint>>();

            for (int i = 0; i < events.Count; ++i)
            {
                ref readonly var eventSent = ref events[i];
                workerSystem.TryGetEntity(eventSent.EntityId, out Entity entity);
                if (!EntityManager.HasComponent<CommandListener>(entity) || EntityManager.GetComponentData<CommandListener>(entity).CommandType == CommandType.None)
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
                NativeQueue<Vector3> potentialReroutes = new NativeQueue<Vector3>(Allocator.TempJob);
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
                NativeQueue<Vector3> potentialReroutes = allPotentialReroutes[scheduleRedirectJob.entityId];

                NativeArray<CollisionSchema.CollisionPoint> collisionPoints = toDispose.First.Value;
                toDispose.RemoveFirst();

                workerSystem.TryGetEntity(scheduleRedirectJob.entityId, out Entity entity);

                // May also jobify this down line. But this does give time for rest of scheduled jobs to complete.
                Vector3? rerouteVector = null;
                while (potentialReroutes.Count > 0)
                {
                    Vector3 potentialRoute = potentialReroutes.Dequeue();

                    bool goodAgainstAll = true;
                    for (int i = 0; i < collisionPoints.Length; ++i)
                    {
                        float dotProduct = Vector3.Dot(
                            potentialRoute.normalized, 
                            HelperFunctions.Vector3fToVector3(collisionPoints[i].Distance).normalized);
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
                if (rerouteVector.HasValue && rerouteVector.Value != Vector3.zero)
                {
                    PositionSchema.LinearVelocity.Component linearVelocityComponent = EntityManager.GetComponentData<PositionSchema.LinearVelocity.Component>(entity);
                    EntityPosition.Component EntityPosition = EntityManager.GetComponentData<EntityPosition.Component>(entity);

                    CommandListener commandListener = EntityManager.GetComponentData<CommandListener>(entity);
                    Debug.Log($"Adding reroute component to {scheduleRedirectJob.entityId}");
                    RerouteComponent rerouteComponent = new RerouteComponent
                    {
                        Destination = commandListener.TargetPosition,
                        SubDestination = HelperFunctions.Vector3fFromUnityVector(rerouteVector.Value),
                        Applied = false,
                        FramesPassed = 0
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