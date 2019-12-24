using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using MDG.DTO;
using MDG.Templates;
using System;
using System.Collections.Generic;
using Unity.Entities;
using CommonSchema = MdgSchema.Common;
using EntityTemplates = MDG.Templates;
using SpawnSchema = MdgSchema.Common.Spawn;

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

        public class SpawnRequestPayload
        {
            public SpawnSchema.SpawnRequest payload;
            public byte[] spawnMetaData;
            public byte[] spawnData;
            public Action<EntityId> callback;
        }

        Queue<SpawnRequestPayload> spawnRequests;
        // So flow is, add to here. Create native array each frame having delays in here.
        // tick down in job, then update by index each one, checking which one to enqueue.
        public delegate void SpawnFulfilledCallback(EntityId spawned);

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



        public void RequestSpawn(SpawnSchema.SpawnRequest spawnRequest, Action<EntityId> spawnFulfilledCallback = null,
            byte[] spawnMetadata = null, byte[] extraSpawnArgs = null)
        {
            var payload = new SpawnRequestPayload
            {
                payload = spawnRequest,
                spawnMetaData = spawnMetadata,
                spawnData = extraSpawnArgs,
                callback = spawnFulfilledCallback,
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

                // Move this to a mapping of type to delegate.
                // special cases being players.
                switch (request.payload.TypeToSpawn)
                {
                    //  So here is where it maybe gets stuck, saw bridge errors, so did it fail to send command?
                    case CommonSchema.GameEntityTypes.Unit:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                EntityTemplates.UnitTemplates.GetUnitEntityTemplate(workerSystem.WorkerId, 
                                request.payload.Position,
                                request.spawnMetaData)
                              ));
                        break;
                    case CommonSchema.GameEntityTypes.Hunted:
                    case CommonSchema.GameEntityTypes.Hunter:
                        sendCreatePlayerRequestSystem.RequestPlayerCreation(request.spawnMetaData,
                            (PlayerCreator.CreatePlayer.ReceivedResponse response) =>
                            {
                                if (response.ResponsePayload.HasValue)
                                {
                                    request.callback?.Invoke(response.ResponsePayload.Value.CreatedEntityId);
                                }
                            });
                        break;
                    case CommonSchema.GameEntityTypes.Resource:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                MDG.Templates.WorldTemplates.GetResourceTemplate()
                            ));
                        break;
                    case CommonSchema.GameEntityTypes.Weapon:
                        WeaponMetadata weaponMetadata = Converters.DeserializeArguments<WeaponMetadata>(request.spawnMetaData);

                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                WeaponTemplates.GetWeaponEntityTemplate(workerSystem.WorkerId, weaponMetadata.weaponType,
                                new EntityId(weaponMetadata.wielderId), weaponMetadata.prefabName, request.spawnData
                                )
                            ));
                        break;
                    case CommonSchema.GameEntityTypes.Structure:
                        requestId = commandSystem.SendCommand(
                           new WorldCommands.CreateEntity.Request(
                               EntityTemplates.StructureTemplates.GetStructureTemplate(workerSystem.WorkerId, request.spawnMetaData, request.payload.Position)));
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
                            spawnRequestHeader.requestInfo.callback?.Invoke(response.EntityId.Value);
                            requestIdToPayload.Remove(response.RequestId);
                            break;
                        case StatusCode.Timeout:
                            // If time out try again on this side, again need to set up generic way of doing these retries.
                            UnityEngine.Debug.Log("Timed out " + response.Message);
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