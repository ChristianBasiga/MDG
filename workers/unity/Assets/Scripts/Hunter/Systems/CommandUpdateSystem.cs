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
using MDG.Hunter.Components;
using MDG.Common.Systems;
using MDG.Common.Components;
using MDG.Logging;

namespace MDG.Hunter.Systems
{
    /// <summary>
    ///  This will be flow, swtich on meta data of command listener.
    ///  Based on switch, it will key to the command monobehaviour.
    ///  the monobehaviour will hold tasks for Behaviour tree, each command is more of a single task rather than a tree.
    ///  But I suppose the process and acting accorindgly could become a behaviour tree.
    /// </summary>
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
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
        private List<CollectPayload> pendingCollects;
        private CommandSystem commandSystem;
        private ResourceRequestSystem resourceRequestSystem;
        private ComponentUpdateSystem componentUpdateSystem;
        private WorkerSystem workerSystem;
        private Dictionary<EntityId, List<EntityId>> unitCollisionMappings;
        private EntityQuery enemyQuery;
        private EntityQuery friendlyQuery;
        public JobHandle CommandExecuteJobHandle { get; private set; }

        private NativeList<CollectResponse> collectResponses;


        // For general reuse of move command job, maybe could have call back to be done?
        // but callbacks aren't blittable.
        public struct MoveCommandJob : IJobForEachWithEntity<MoveCommand, EntityTransform.Component>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref MoveCommand moveCommand, [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);
                float distance = math.distance(moveCommand.destination, pos);
                const float minDistance = 1.0f;
                if (distance < minDistance)
                {
                    entityCommandBuffer.RemoveComponent(jobIndex, entity, typeof(MoveCommand));
                }
                else
                {
                    Vector3f destinationVector = new Vector3f(moveCommand.destination.x, moveCommand.destination.y, moveCommand.destination.z);
                    entityTransform.Position = entityTransform.Position + (destinationVector - entityTransform.Position) * deltaTime;
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
            public NativeList<CollectPayload> collectPayloads;
            public void Execute(Entity entity, int jobIndex, ref SpatialEntityId spatialEntityId, ref CollectCommand collectCommand, ref EntityTransform.Component entityTransform)
            {
                float3 pos = new float3(entityTransform.Position.X, entityTransform.Position.Y, entityTransform.Position.Z);
                float distance = math.distance(collectCommand.destination, pos);
                const float minDistance = 1.0f;
                // When should I mark IsAtResource
                if (!collectCommand.IsAtResource)
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
                    // For trigger animation.
                    collectCommand.IsCollecting = true;
                    // Otherwise we can begin collecting.
                    collectPayloads.Add(new CollectPayload { requestingOccupant = spatialEntityId.EntityId, resourceId = collectCommand.resourceId });
                }
            }
        }

        
        /// <summary>
        /// Flow of collecting:
        /// - Move to resource, job on the Unit
        /// - Start collecting of resource, units should be set to collecting(trigger animation) and resource needs to know that it is occupied.
        ///     - Maybe transfer from map and push to list that are done collecting. Or map of resource type.
        ///     - Then another job loops through units, collect command, and inventory component. Then simply add an item of htat resource type.
        ///     - Gotta figure out inventory for now, but that should work.
        /// - Put resource into inventory.
        /// - despawn resource.
        /// 
        /// </summary>


        protected override void OnCreate()
        {
            base.OnCreate();
            pendingCollects = new List<CollectPayload>();
            collectResponses = new NativeList<CollectResponse>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            resourceRequestSystem = World.GetExistingSystem<ResourceRequestSystem>();
            resourceRequestSystem.OnCollect += HandleCollectResponse;
            commandSystem = World.GetExistingSystem<CommandSystem>();
            unitCollisionMappings = new Dictionary<EntityId, List<EntityId>>();
            enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
            friendlyQuery = GetEntityQuery(ComponentType.ReadOnly<CommandListener>(), ComponentType.ReadOnly<SpatialEntityId>(), ComponentType.ReadOnly<Position.Component>());
        }

        // Could do PostUpdate here, instead of jobifying.
        // it is essentially in separate thread regardless just doesn't have burst benefits.
        // If it becomes problem I'll translate it to that for now. Just update directly.
        private void HandleCollectResponse(CollectResponse receivedResponse)
        {
            // Then it is depleted.
            if (receivedResponse.TimesUntilDepleted == 0)
            {
                if (workerSystem.TryGetEntity(receivedResponse.DepleterId, out Entity entity))
                {
                    PostUpdateCommands.RemoveComponent<CollectCommand>(entity);
                    // So what this needs to do is set pending inventory addition component.
                    // That will be temp component to remove next frame.
                    // Then diff system will be running jobs to add to actual inventory.
                    // a frame off for adding to inventory not huge deal and keeps it clean.
                    PostUpdateCommands.AddComponent(entity, new PendingInventoryAddition { InventoryItemId = receivedResponse.ResourceId });
                }
                // I mean this HAS to be true for us to get this response.
                if (pendingCollects.Count > 0)
                {
                    foreach (CollectPayload collectPayload in pendingCollects)
                    {
                        // If not the depleter, then just interrupt their stuff without adding to inventory.
                        if (!collectPayload.requestingOccupant.Equals(receivedResponse.DepleterId))
                        {
                            if (workerSystem.TryGetEntity(collectPayload.requestingOccupant, out Entity interruptedEntity))
                            {

                                //Test this.
                                PostUpdateCommands.RemoveComponent<CollectCommand>(interruptedEntity);
                            }
                        }
                    }
                }
                // Along with this need to remove all CollectCommands that have this depleted resource as id.
                // That has to be a job. List is stll needed.
            }
            
            // So then here adds to native list.
            collectResponses.Add(receivedResponse);
        }


        protected override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            MoveCommandJob moveCommandJob = new MoveCommandJob
            {
                deltaTime = deltaTime,
                // Need to be able to get post update commands in client world in job component system.
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            moveCommandJob.Schedule(this).Complete();

            // Move to own function or even its own system and make a system group.
            /*
            int capacity = EntityManager.CreateEntityQuery(typeof(CollectCommand)).CalculateEntityCount();
            NativeList<CollectPayload> pendingCollects = new NativeList<CollectPayload>(capacity, Allocator.Persistent);

            // Travel to resource jobs.
            MoveToResourceJob moveToResourceJob = new MoveToResourceJob
            {
                deltaTime = deltaTime,
                collectPayloads = pendingCollects
            };
            // For each pending collect, send Collect request.
            collectJobHandle = moveToResourceJob.Schedule(this);

            // Could be a job I run every few frames since won't change or simply on create of this system.

            //So instead of collect payload, maybe resourcerequestheader?
            foreach (CollectPayload collectPayload in pendingCollects)
            {
                ResourceRequestSystem.ResourceRequestHeader payload = new ResourceRequestSystem.ResourceRequestHeader
                {
                    OccupantId = collectPayload.requestingOccupant,
                    ResourceId = collectPayload.resourceId,
                    ResourceRequestType = ResourceRequestType.COLLECT
                };
                resourceRequestSystem.SendRequest(payload);
            }

            // So maybe I could reuse this. Pending Collects have all info needed.
            // and makes sense. Just cause sent request doesn't mean not still pending.
            // it is pending till gets response, so should store it.

            // List is fine, it's not HUGE list in single frame ever I don't thnk.
            // Or rather not huge enough to worry about that. That is not bottle neck.
            // but maybe make it a hash table for performance later if need be.
            this.pendingCollects.AddRange(pendingCollects.ToArray());
            */
           // pendingCollects.Dispose();
        }
    }
}