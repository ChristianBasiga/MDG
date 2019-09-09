
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using System.Collections.Generic;
//using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using MDG.Hunter;
using MdgSchema.Units;
using UnityEngine;
using MdgSchema.Spawners;
using Improbable;
using Unity.Jobs;
using Unity.Collections;
using MDG.Hunter.Components;
using Unity.Rendering;
using Zenject;
using MDG.ClientSide;
using MDG.Common.Components;
using Unity.Mathematics;

namespace MDG.Hunter.Systems.UnitCreation
{

    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [DisableAutoCreation]
    //This should actually also be on client side only.
    public class UnitCreationRequestSystem : ComponentSystem
    {
        private CommandSystem commandSystem;
        private EntitySystem entitySystem;
        private WorkerSystem workerSystem;
        private EntityQuery filter;
        private EntityQuery unitSetupQuery;
        private Dictionary<long, Coordinates> creationRequestToPosition;
        private Dictionary<EntityId, Coordinates> entityIdToPosition;

        // Maybe this will be stored else where, like a mesh factory.
        private Dictionary<UnitTypes, RenderMesh> unitTypeToRenderMesh;

        public void Initialize(Dictionary<UnitTypes, RenderMesh> unitTypeToRenderMesh)
        {
            this.unitTypeToRenderMesh = unitTypeToRenderMesh;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            filter = GetEntityQuery(ComponentType.ReadWrite<UnitSpawner.Component>());
            unitSetupQuery = GetEntityQuery(
                ComponentType.ReadOnly<MdgSchema.Units.Unit.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            creationRequestToPosition = new Dictionary<long, Coordinates>();
            entityIdToPosition = new Dictionary<EntityId, Coordinates>();

        }

        protected override void OnUpdate()
        {
            ProcessUnitCreation();
            ProcessUnitSetup();
        }


        private void ProcessUnitCreation()
        {
            // Queues all Units to spawn.
            // Instead of iterating through unit spawners, 
            Entities.With(filter).ForEach((ref SpatialEntityId spatialEntityId, ref UnitSpawner.Component spawner) =>
            {
                if (spawner.AmountToSpawn > 0)
                {
                    
                    EntityTemplate template = Unit.Templates.GetCollectorUnitEntityTemplate(workerSystem.WorkerId);
                    long requestId = commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(template));
                    creationRequestToPosition[requestId] = spawner.Position;
                    commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request(spatialEntityId.EntityId));
                }
                spawner.AmountToSpawn = 0;
            });

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
                        switch (response.StatusCode)
                        {
                            case StatusCode.Success:
                                entityIdToPosition[response.EntityId.Value] = positionForUnit;
                                creationRequestToPosition.Remove(response.RequestId);
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


        private void ProcessUnitSetup()
        {
            if (entityIdToPosition.Count == 0) return;
            UnityClientConnector gm = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            CustomGameObjectCreator creator = gm.customGameObjectCreator;
            Debug.LogError(creator);
            //Mayhaps, I can set hte acive world to client world??
            foreach (var world in World.AllWorlds)
            {
                Debug.LogError(world.Name);
            }
            Entities.With(unitSetupQuery).ForEach((ref SpatialEntityId id, ref MdgSchema.Units.Unit.Component unitComponent) =>
            {
                
                if (entityIdToPosition.TryGetValue(id.EntityId, out Coordinates positionToSet))
                {
                    if (creator.EntityToGameObjects.TryGetValue(id.EntityId, out List<GameObject> gameobjects))
                    {
                        Debug.LogError(gameobjects[0].name);
                        Debug.LogError(gameobjects[0].transform.position);
                        Debug.LogError(positionToSet);
                        Debug.LogError(positionToSet.ToUnityVector());
                        gameobjects[0].transform.position = positionToSet.ToUnityVector();
                        entityIdToPosition.Remove(id.EntityId);
                    }

                }
            });
        }

        private void ProcessMeshRenderSetup()
        {


        }
    }
}