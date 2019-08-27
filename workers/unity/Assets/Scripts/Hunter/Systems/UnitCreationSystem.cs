using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MdgSchema.Spawners;
using Improbable.Gdk.Core;
using MDG.Hunter.Unit;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
namespace MDG.Hunter.Systems.UnitCreation {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UnitCreationSystem : ComponentSystem
    {
        // This needs to create it in two places.
        // game client's world and game logic's world.
        private CommandSystem commandSystem;
        //For knowing who to send response back to in request response cycle.
        private class UnitCreationContext
        {
            public UnitSpawner.SpawnUnit.ReceivedRequest unitCreationRequest;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetOrCreateSystem<CommandSystem>();

        }

        protected override void OnUpdate()
        {

            #region Handle requests and send create requests.
            var creationRequests = commandSystem.GetRequests<UnitSpawner.SpawnUnit.ReceivedRequest>();

            for (int i = 0; i < creationRequests.Count; ++i)
            {
                Debug.LogError("iterating through requests.");

                ref readonly var request = ref creationRequests[i];
                EntityTemplate unitTemplate;
                switch (request.Payload.Type)
                {
                    case UnitTypes.COLLECTOR:
                        Debug.LogError("got collector");
                        unitTemplate = Unit.Templates.GetUnitEntityTemplate(request.CallerWorkerId);
                        break;
                    default:
                        unitTemplate = Unit.Templates.GetUnitEntityTemplate(request.CallerWorkerId);
                        break;
                }

                Debug.LogError("spawning collector");
                commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(
                    unitTemplate,
                    context: new UnitCreationContext { unitCreationRequest = request }
                ));

            }
            #endregion

            #region Process responses from entity creation and send back response to SpawnUnitRequester.
            //Then we've sent a request, check responses.
            var entityCreationResponses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (int i = 0; i < entityCreationResponses.Count; ++i)
            {
                ref readonly var response = ref entityCreationResponses[i];

                if (!(response.Context is UnitCreationContext requestContext))
                {
                    continue;
                }

                if (response.StatusCode != StatusCode.Success)
                {
                    commandSystem.SendResponse(new UnitSpawner.SpawnUnit.Response
                    {
                        RequestId = requestContext.unitCreationRequest.RequestId,
                        Payload = new UnitSpawnResponse { Spawned = false },
                        FailureMessage = $"Failed to spawn {requestContext.unitCreationRequest.Payload.Type.ToString()} {response.Message}"
                    });
                }
                else
                {
                    //This needs reference to boh worlds.
                    commandSystem.SendResponse(new UnitSpawner.SpawnUnit.Response
                    {
                        RequestId = requestContext.unitCreationRequest.RequestId,
                        Payload = new UnitSpawnResponse { Spawned = true },
                    });
                }

            }
            #endregion
        }
    }
}