using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using SpawnSchema = MdgSchema.Common.Spawn;
using CommonSchema = MdgSchema.Common;
using Improbable.Gdk.PlayerLifecycle;
using MdgSchema.Common;
using System;

namespace MDG.Common.Systems.Spawn
{

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class SpawnRequestSystem : ComponentSystem
    {
        CommandSystem commandSystem;
        // Specialized system for linking player life cycle and heart beat tracking.
        SendCreatePlayerRequestSystem sendCreatePlayerRequestSystem;
        WorkerSystem workerSystem;
        Dictionary<long, SpawnRequestHeader> requestIdToPayload;
        Queue<SpawnRequestPayload> spawnRequests;
        public delegate void SpawnFulfilledCallback(EntityId spawned);

        public class SpawnRequestPayload
        {
            public SpawnSchema.SpawnRequest payload;
            public Action<EntityId> callback;
        }

        public class SpawnRequestHeader
        {
            public long requestId;
            public SpawnRequestPayload requestInfo;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            spawnRequests = new Queue<SpawnRequestPayload>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            sendCreatePlayerRequestSystem = workerSystem.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();

            requestIdToPayload = new Dictionary<long, SpawnRequestHeader>();
        }

        public void RequestSpawn(SpawnSchema.SpawnRequest spawnRequest, Action<EntityId> spawnFulfilledCallback = null)
        {
            var payload = new SpawnRequestPayload
            {
                payload = spawnRequest,
                callback = spawnFulfilledCallback
            };
            spawnRequests.Enqueue(payload);
        }

        protected override void OnUpdate()
        {
            try
            {
                ProcessRequests();
                ProcessResponses();
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.Log(exception);
            }
        }

        private void ProcessRequests()
        {
            while (spawnRequests.Count > 0)
            {
                var request = spawnRequests.Dequeue();
                long requestId = -1;
                switch (request.payload.TypeToSpawn)
                {
                    case CommonSchema.GameEntityTypes.Unit:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                MDG.Hunter.Unit.Templates.GetUnitEntityTemplate(workerSystem.WorkerId, request.payload.TypeId)
                              ));
                        break;
                    case CommonSchema.GameEntityTypes.Hunted:
                    case CommonSchema.GameEntityTypes.Hunter:
                        DTO.PlayerConfig playerConfig = new DTO.PlayerConfig
                        {
                            playerType = request.payload.TypeToSpawn
                        };
                        sendCreatePlayerRequestSystem.RequestPlayerCreation(DTO.Converters.SerializeArguments(playerConfig),
                            (PlayerCreator.CreatePlayer.ReceivedResponse response) =>
                            {
                                request.callback.Invoke(response.ResponsePayload.Value.CreatedEntityId);
                            }
                            );
                        break;
                    case CommonSchema.GameEntityTypes.Resource:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                MDG.Common.Templates.GetResourceTemplate()
                            ));
                        break;
                }
                if (requestId != -1)
                {
                    requestIdToPayload[requestId] = new SpawnRequestHeader
                    {
                        requestId = requestId,
                        requestInfo = request
                    };
                }
            }
        }
        private void ProcessResponses()
        {
            if (requestIdToPayload.Count == 0)
            {
                return;
            }
            var creationResponses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (int i = 0; i < creationResponses.Count; ++i)
            {
                ref readonly var response = ref creationResponses[i];
                if (requestIdToPayload.TryGetValue(response.RequestId, out SpawnRequestHeader spawnRequestHeader))
                {
                    switch (response.StatusCode)
                    {
                        // Remove from request mappings and send response back.
                        case StatusCode.Success:

                            if (workerSystem.TryGetEntity(response.EntityId.Value, out Unity.Entities.Entity entity))
                            {
                                EntityManager.SetComponentData(entity, new EntityTransform.Component
                                {
                                    Position = spawnRequestHeader.requestInfo.payload.Position
                                });
                            }
                            spawnRequestHeader.requestInfo.callback?.Invoke(response.EntityId.Value);
                            requestIdToPayload.Remove(response.RequestId);
                            break;
                        case StatusCode.Timeout:
                            // If time out try again on this side, again need to set up generic way of doing these retries.
                            break;
                        default:
                            commandSystem.SendResponse(new SpawnSchema.SpawnManager.SpawnGameEntity.Response
                            {
                                RequestId = spawnRequestHeader.requestId,
                                FailureMessage = $"{response.StatusCode.ToString()}: {response.Message}"
                            });
                            break;
                    }
                }
            }
        }
    }
}