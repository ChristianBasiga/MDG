using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Mathematics;
using MdgSchema.Game.Resource;
using MDG.Invader.Components;
using MDG.Common.Systems;
using MDG.Common.Components;
using MDG.Logging;
using PositionSchema = MdgSchema.Common.Position;
using PointSchema = MdgSchema.Common.Point;
using MDG.Common.Systems.Point;
using Improbable.Gdk.Subscriptions;

namespace MDG.Invader.Systems
{
    /// <summary>
    ///  This will be flow, swtich on meta data of command listener.
    ///  Based on switch, it will key to the command monobehaviour.
    ///  the monobehaviour will hold tasks for Behaviour tree, each command is more of a single task rather than a tree.
    ///  But I suppose the process and acting accorindgly could become a behaviour tree.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class CommandUpdateSystem : ComponentSystem
    {

        public struct CollectPayload
        {
            public EntityId resourceId;
            public EntityId requestingOccupant;
        }

        private EntityQuery collectorGroup;

        private bool assignedJobHandle = false;
        private JobHandle collectJobHandle;
        private NativeQueue<CollectPayload> pendingOccupy;
        private NativeQueue<CollectPayload> pendingCollects;
        private NativeQueue<CollectPayload> pendingReleases;

        private CommandSystem commandSystem;
        private ResourceRequestSystem resourceRequestSystem;
        private ComponentUpdateSystem componentUpdateSystem;
        private WorkerSystem workerSystem;
        private Dictionary<EntityId, List<EntityId>> unitCollisionMappings;
        private EntityQuery applyingMoveCommandGroup;
        private EntityQuery interruptedGroup;
        private EntityQuery enemyQuery;
        private EntityQuery friendlyQuery;
        public JobHandle CommandExecuteJobHandle { get; private set; }

        private NativeList<CollectResponse> collectResponses;


        public struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityTransform.Component, PositionSchema.LinearVelocity.Component>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref MoveCommand moveCommand, [ReadOnly] ref EntityTransform.Component entityTransform,
                ref PositionSchema.LinearVelocity.Component linearVelocityComponent)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);
                float distance = math.distance(moveCommand.destination, pos);
                const float minDistance = 1.0f;
                if (distance < minDistance)
                {
                    linearVelocityComponent.Velocity = Vector3f.Zero;
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
                else
                {
                    Vector3f direction = new Vector3f(moveCommand.destination.x, entityTransform.Position.Y, moveCommand.destination.z) - entityTransform.Position;
                    //All commands overwrite any other effect on velocity
                    linearVelocityComponent.Velocity = direction;
                }
            }
        }


        // Collect could still use move like before.
        // then adds to map, for actual collecting to take place
        // but that's onl if has both. Which I could query on command meta data, but not as clean.
        // that will iterate on all resources.
        public struct MoveToResourceJob : IJobForEachWithEntity<SpatialEntityId, CollectCommand, EntityTransform.Component>
        {
            // There is chance that the resource moving to is gone before get there.
            // In that case collect command needs to removed on not just those ready to collect but on all.
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            [WriteOnly]
            public NativeQueue<CollectPayload>.ParallelWriter occupyPayloads;
            public void Execute(Entity entity, int jobIndex, ref SpatialEntityId spatialEntityId, ref CollectCommand collectCommand, ref EntityTransform.Component entityTransform)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);
                float distance = math.distance(collectCommand.destination, pos);
                const float minDistance = 1.0f;
                // When should I mark IsAtResource
                if (!collectCommand.IsCollecting && !collectCommand.IsAtResource)
                {
                    if (distance < minDistance)
                    {
                        collectCommand.IsAtResource = true;
                    }
                    else
                    {
                        Vector3f destinationVector = new Vector3f(collectCommand.destination.x, collectCommand.destination.y, collectCommand.destination.z);
                        entityTransform.Position = entityTransform.Position + (destinationVector - entityTransform.Position) * deltaTime;
                    }
                }
                else
                {
                    // So once this happens, it will start sending collect requests form payload.
                    // For trigger animation.
                    // Maybe could add transient components to better do these in sync.
                    // TryingOccupy, TryingCollect, etc. For now it's fine just check isCollecting
                    // Former thought would be cleanest.

                    collectCommand.IsCollecting = true;
                    // Otherwise we can begin collecting.
                    occupyPayloads.Enqueue(new CollectPayload { requestingOccupant = spatialEntityId.EntityId, resourceId = collectCommand.resourceId });
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            applyingMoveCommandGroup = GetEntityQuery(
                ComponentType.ReadOnly<CommandListener>(),
                ComponentType.ReadWrite<PositionSchema.LinearVelocity.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.ComponentAuthority>(),
                ComponentType.ReadOnly<MoveCommand>(),
                ComponentType.ReadOnly<EntityTransform.Component>()
                );
            applyingMoveCommandGroup.SetFilter(PositionSchema.LinearVelocity.ComponentAuthority.Authoritative);

            pendingCollects = new NativeQueue<CollectPayload>(Allocator.Persistent);
            pendingOccupy = new NativeQueue<CollectPayload>(Allocator.Persistent);
            collectResponses = new NativeList<CollectResponse>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            resourceRequestSystem = World.GetExistingSystem<ResourceRequestSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            unitCollisionMappings = new Dictionary<EntityId, List<EntityId>>();
            interruptedGroup = GetEntityQuery(ComponentType.ReadOnly<CommandInterrupt>(), ComponentType.ReadOnly<SpatialEntityId>());
            enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
            friendlyQuery = GetEntityQuery(ComponentType.ReadOnly<CommandListener>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            pendingCollects.Dispose();
            pendingOccupy.Dispose();
        }

        private void HandleCollectCommandResponse(ResourceRequestSystem.ResourceRequestReponse receivedResponse)
        {
            // Simplest way is let them continue to it, then send occupy request and simply fail that way instead of interrupting mid way
            // otherwies would have to iterate past it.
            if (receivedResponse.OccupyResponse.HasValue)
            {
                var occupyResponse = receivedResponse.OccupyResponse.Value;
                if (occupyResponse.Occupied)
                {
                    // Need to re think this 
                    // Add to collecting
                    pendingCollects.Enqueue(new CollectPayload
                    {
                        requestingOccupant = occupyResponse.OccupantId,
                        resourceId = occupyResponse.ResourceId,
                    });
                }
                else if (occupyResponse.FullyOccupied)
                {
                    //Then remove component.
                    if (workerSystem.TryGetEntity(occupyResponse.OccupantId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;
                        PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                    }
                }
            }
            else if (receivedResponse.CollectResponse.HasValue)
            {
                var collectResponse = receivedResponse.CollectResponse.Value;

                if (collectResponse.TimesUntilDepleted == 0)
                {
                    if (collectResponse.DepleterId.HasValue)
                    {
                        // Irrelevant now since units no longer have inventory, but maybe down line
                        // increase collect skill to increase rate or whatever.
                        EntityId depleter = collectResponse.DepleterId.Value;

                        if (depleter.Equals(collectResponse.OccupantId))
                        {
                            // Do something.
                        }

                    }
                    if (workerSystem.TryGetEntity(collectResponse.OccupantId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;
                        PostUpdateCommands.RemoveComponent<CollectCommand>(entity);

                    }
                    // Resource depleted so remove it.
                }
                else
                {
                    // Continue collecting.
                    pendingCollects.Enqueue(new CollectPayload
                    {
                        requestingOccupant = collectResponse.OccupantId,
                        resourceId = collectResponse.ResourceId,
                    });
                }

                // Then add points to Invader.
                if (receivedResponse.Success)
                {
                    // Get resource point value.
                    if (workerSystem.TryGetEntity(collectResponse.ResourceId, out Entity entity))
                    {
                        EntityManager entityManager = workerSystem.EntityManager;
                        PointSchema.PointMetadata.Component pointMetadata = entityManager.GetComponentData<PointSchema.PointMetadata.Component>(entity);
                        // Replace this with getting from zenject store.
                        GameObject invaderObject = GameObject.Find("Hunter_Spawned");
                        PointRequestSystem pointRequestSystem = World.GetExistingSystem<PointRequestSystem>();
                        pointRequestSystem.AddPointRequest(new MdgSchema.Common.Point.PointRequest
                        {
                            EntityUpdating = invaderObject.GetComponent<LinkedEntityComponent>().EntityId,
                            PointUpdate = pointMetadata.StartingPoints
                        });
                    }
                }
            }
            else if (receivedResponse.ReleaseResponse.HasValue)
            {
                var releaseResponse = receivedResponse.ReleaseResponse.Value;
                if (receivedResponse.Success)
                {
                    if (workerSystem.TryGetEntity(releaseResponse.Occupant, out Entity entity))
                    {
                        PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                    }
                }
            }
        }


        protected override void OnUpdate()
        {
            RunCommandJobs();
            RunCommandRequests();
            ProcessInterruptedCommands();
        }

        private void RunCommandJobs()
        {
            float deltaTime = Time.deltaTime;
            MoveCommandJob moveCommandJob = new MoveCommandJob
            {
                deltaTime = deltaTime,
                // Need to be able to get post update commands in client world in job component system.
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            moveCommandJob.Schedule(applyingMoveCommandGroup).Complete();

            MoveToResourceJob moveToResourceJob = new MoveToResourceJob
            {
                deltaTime = deltaTime,
                occupyPayloads = pendingOccupy.AsParallelWriter()
            };
            // For each pending collect, send Collect request.
            moveToResourceJob.Schedule(this).Complete();
        }

        private void RunCommandRequests()
        {
            while (pendingOccupy.Count > 0)
            {
                var occupyPayload = pendingOccupy.Dequeue();
                resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                {
                    ResourceRequestType = ResourceRequestType.OCCUPY,
                    OccupantId = occupyPayload.requestingOccupant,
                    ResourceId = occupyPayload.resourceId,
                    callback = HandleCollectCommandResponse,
                });
            }
            while (pendingCollects.Count > 0)
            {
                var collectPayload = pendingCollects.Dequeue();
                resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                {
                    ResourceRequestType = ResourceRequestType.COLLECT,
                    OccupantId = collectPayload.requestingOccupant,
                    ResourceId = collectPayload.resourceId,
                    callback = HandleCollectCommandResponse
                });
            }

        }

        // Main issue with this is that it's doing too much.
        // Tbh, for stuff like animations, it should be diff system also acting on CommandInterrupt.
        private void ProcessInterruptedCommands()
        {
           Entities.With(interruptedGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId,  ref CommandInterrupt commandInterrupted) =>
           {
               switch (commandInterrupted.interrupting)
               {
                   // Fall through because I am treating everything as a 'resource' so units focusing on an enemy
                   // are 'occupying' it. When unit is disarming a trap, it is 'occupying' the resource.
                   // Collect in itself is very generic just 'health' but health could mean anything.
                   case Commands.CommandType.Attack:
                      
                   case Commands.CommandType.Collect:
                       resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                       {
                           OccupantId = spatialEntityId.EntityId,
                           ResourceId = commandInterrupted.target.Value,
                           ResourceRequestType = ResourceRequestType.RELEASE
                       });
                       break;
                   case Commands.CommandType.Move:
                       break;
               }
           });
        }
    }
}