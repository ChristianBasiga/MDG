
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
using MdgSchema.Common;

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
        private Dictionary<long, Vector3f> creationRequestToPosition;
        private Dictionary<EntityId, Vector3f> entityIdToPosition;

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
            filter = GetEntityQuery(ComponentType.ReadWrite<Components.UnitSpawner>());
            unitSetupQuery = GetEntityQuery(
                ComponentType.ReadOnly<MdgSchema.Units.Unit.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<EntityTransform.Component>(),
                ComponentType.ReadOnly<EntityTransform.ComponentAuthority>()
            );
            unitSetupQuery.SetFilter(EntityTransform.ComponentAuthority.Authoritative);
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            creationRequestToPosition = new Dictionary<long, Vector3f>();
            entityIdToPosition = new Dictionary<EntityId, Vector3f>();

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
            Entities.With(filter).ForEach((Unity.Entities.Entity entity, ref Components.UnitSpawner spawner) =>
            {
                if (spawner.AmountToSpawn > 0)
                {
                    
                    EntityTemplate template = Unit.Templates.GetCollectorUnitEntityTemplate(workerSystem.WorkerId);
                    long requestId = commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(template));
                    creationRequestToPosition[requestId] = spawner.Position;
                    spawner.AmountToSpawn = 0;
                }
                else
                {
                    PostUpdateCommands.DestroyEntity(entity);
                }
            });

            //Then query create entity responses in client world and look for unit creations.
            if (creationRequestToPosition.Count > 0)
            {
                var responses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
                for (int i = 0; i < responses.Count; ++i)
                {
                    ref readonly var response = ref responses[i];
                    //Map entityId to position for that entity.
                    if (creationRequestToPosition.TryGetValue(response.RequestId, out Vector3f positionForUnit))
                    {
                        switch (response.StatusCode)
                        {
                            case StatusCode.Success:
                                entityIdToPosition[response.EntityId.Value] = positionForUnit;
                                creationRequestToPosition.Remove(response.RequestId);
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
            Entities.With(unitSetupQuery).ForEach((ref SpatialEntityId id, ref MdgSchema.Units.Unit.Component unitComponent, ref EntityTransform.Component position) =>
            {
                if (entityIdToPosition.TryGetValue(id.EntityId, out Vector3f positionToSet))
                {
                    position.Position = positionToSet;
                    entityIdToPosition.Remove(id.EntityId);
                    if (creator.EntityToGameObjects.TryGetValue(id.EntityId, out List<GameObject> gameObjects))
                    {
                        GameObject gameObject;
                        if (GameObject.Find(gameObjects[0].name))
                        {
                            gameObject = gameObjects[0];
                        }
                        else
                        {
                            gameObject = gameObjects[1];
                        }
                         gameObject.transform.position = positionToSet.ToUnityVector();
                    }

                }
            });
        }

        private void ProcessMeshRenderSetup()
        {


        }
    }
}