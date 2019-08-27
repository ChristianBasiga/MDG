
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using System.Collections.Generic;
//using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using MDG.Hunter;
using MdgSchema.Lobby;
using System.Linq;
using UnityEngine.Scripting;
using UnityEngine;
using MdgSchema.Spawners;
using Improbable;
using Unity.Jobs;
using Unity.Collections;
using MDG.Hunter.Components;

namespace MDG.Hunter.Systems.UnitCreation
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    //This should actually also be on client side only.
    public class UnitCreationRequestSystem : ComponentSystem
    {
        private CommandSystem commandSystem;
        private EntitySystem entitySystem;
        private WorkerSystem workerSystem;
        private EntityQuery filter;
        private Dictionary<long, Coordinates> creationRequestToPosition;
        private Dictionary<EntityId, Coordinates> entityIdToPosition;


        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            filter = GetEntityQuery(ComponentType.ReadWrite<UnitSpawner.Component>());
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            creationRequestToPosition = new Dictionary<long, Coordinates>();
            entityIdToPosition = new Dictionary<EntityId, Coordinates>();
        }

        protected override void OnUpdate()
        {
            ProcessUnitCreation();
            ProcessUnitPositionSetup();
        }

        private void ProcessUnitRequests()
        {
            // Bit harder to think about.
        }

        private void ProcessUnitCreation()
        {
            // Queues all Units to spawn.
            //Instead of iterating through unit spawners, 
            Entities.With(filter).ForEach((ref UnitSpawner.Component spawner) =>
            {
                if (spawner.AmountToSpawn > 0)
                {
                    EntityTemplate template = Unit.Templates.GetUnitEntityTemplate(workerSystem.WorkerId);
                    long requestId = commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(template));
                    creationRequestToPosition[requestId] = spawner.Position;
                }               
                spawner.AmountToSpawn = 0;
            });
            PostUpdateCommands.DestroyEntity(filter);
            //Then query create entity responses in client world and look for unit creations.
            if (creationRequestToPosition.Count > 0)
            {
                var responses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
                for (int i = 0; i < responses.Count; ++i)
                {
                    ref readonly var response = ref responses[i];
                    //if matched, then we got response.
                    Coordinates positionForUnit;

                    //Map entityId to position for that entity.
                    if (creationRequestToPosition.TryGetValue(response.RequestId, out positionForUnit))
                    {
                        creationRequestToPosition.Remove(response.RequestId);
                        switch (response.StatusCode)
                        {
                            case StatusCode.Success:
                                entityIdToPosition[response.EntityId.Value] = positionForUnit;
                                Debug.LogError($"created unit {response.Message}");
                                break;
                            default:
                                Debug.LogError($"failed to create unit {response.Message}");
                                break;
                        }
                    }
                }
            }
        }


        private void ProcessUnitPositionSetup()
        {
            if (entityIdToPosition.Count == 0) return;
            //This should work, we'll see.
            EntityQuery positionQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<Position.Component>(),
                ComponentType.ReadOnly<UnitComponent>());

            Entities.With(positionQuery).ForEach((ref SpatialEntityId id, ref Position.Component position) =>
            {
                Coordinates positionToSet;
                if (entityIdToPosition.TryGetValue(id.EntityId, out positionToSet))
                {

                    position.Coords = positionToSet;
                    entityIdToPosition.Remove(id.EntityId);
                }
            });
            // After setup, at end of every frame, remove a unit spawner for new ones to come in.
            // Should happen after use unit spawner to make request.
        }
    }
}