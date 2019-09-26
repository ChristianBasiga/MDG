using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using SpawnSchema = MdgSchema.Common.Spawn;
using CommonSchema = MdgSchema.Common;
namespace MDG.Common.Systems.Spawn
{
    public class SpawnRequestSystem : ComponentSystem
    {
        CommandSystem commandSystem;
        WorkerSystem workerSystem;
        Dictionary<long, SpawnRequestHeader> requestIdToPayload;

        public struct SpawnRequestHeader
        {
            public long requestId;
            public SpawnSchema.SpawnRequest payload;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            requestIdToPayload = new Dictionary<long, SpawnRequestHeader>();
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
            // replace 25 with actual worker later.
            var requests = commandSystem.GetRequests<SpawnSchema.SpawnManager.SpawnGameEntity.ReceivedRequest>(new EntityId(25));
            for (int i = 0; i < requests.Count; ++i)
            {
                ref readonly var request = ref requests[i];

                SpawnSchema.SpawnRequest payload = request.Payload;
                long requestId = -1;
                switch (payload.TypeToSpawn)
                {
                    case CommonSchema.GameEntityTypes.Unit:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                MDG.Hunter.Unit.Templates.GetUnitEntityTemplate(workerSystem.WorkerType, payload.TypeId)
                              ));
                        break;
                    // Todo: Add other cases, ie: building. Defender
                    // Defender is new Hunted, gotta make time to rename all this.
                    case CommonSchema.GameEntityTypes.Hunted:
                        // This one will be similiar call that I made in GameManager, which I also need to update.
                        break;
                }
                if (requestId != -1)
                {
                    requestIdToPayload[requestId] = new SpawnRequestHeader
                    {
                        requestId = request.RequestId,
                        payload = payload
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
                            requestIdToPayload.Remove(response.RequestId);
                            commandSystem.SendResponse(new SpawnSchema.SpawnManager.SpawnGameEntity.Response
                            {
                                RequestId = spawnRequestHeader.requestId,
                                Payload = new SpawnSchema.SpawnResponse(response.EntityId.Value)
                            });
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