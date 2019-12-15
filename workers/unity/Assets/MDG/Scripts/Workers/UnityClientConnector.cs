using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using MDG.Common.Systems;
using MDG.Invader.Systems;
using UnityEngine;
using Improbable;
using Unity.Entities;
using MDG.Templates;
using MDG.Common.Systems.Inventory;
using MDG.Common.Systems.Spawn;
using MDG.Common.Systems.Point;
using MDG.Common.Systems.Weapon;
using MDG.Common.Systems.Structure;
using GameSchema = MdgSchema.Game;
using MDG.ScriptableObjects.Game;
using System.Collections;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common;
using MDG.Common;
using MDG.DTO;
using Improbable.Gdk.Subscriptions;
using System.Collections.Generic;

namespace MDG
{
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        public GameEntityTypes PlayerRole { private set; get; }
        public ClientGameObjectCreator ClientGameObjectCreator { get; private set; }
        public GameConfig GameConfig { private set; get; }

        public SpatialOSEntity GameManagerEntity { private set; get; }

        public List<SpatialOSEntity> TerritoryEntities { private set; get; }

        public bool PlayerFinishedLoading { private set; get; }

        MainOverlayHUD mainOverlayHUD;

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerType);
            connParams.Network.ConnectionType = NetworkConnectionType.Kcp;
            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType), initializer));
                        break;
                    case ConnectionService.Locator:
                        var locator = new LocatorFlow(initializer);
                        builder.SetConnectionFlow(locator);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType)));
            }

            PlayerLifecycleConfig.CreatePlayerEntityTemplate = PlayerTemplates.CreatePlayerEntityTemplate;

            // Replace with loading config from connection param
            GameConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseGameConfig") as GameConfig;
            mainOverlayHUD = GetComponent<MainOverlayHUD>();
            mainOverlayHUD.OnRoleSelected += SpawnPlayer;
            TerritoryEntities = new List<SpatialOSEntity>();
            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }


        private void OnNewEntityAdded(SpatialOSEntity spatialOSEntity)
        {
            spatialOSEntity.TryGetComponent(out Metadata.Component metadata);

            switch (metadata.EntityType)
            {
                case "GameManager":
                    GameManagerEntity = spatialOSEntity;
                    break;
                case "Territory":
                    TerritoryEntities.Add(spatialOSEntity);
                    break;
            }
        }

        

        private void SpawnPlayer(MdgSchema.Common.GameEntityTypes type)
        {
            Vector3 position = GameConfig.DefenderSpawnPoints[0];
            PlayerRole = type;
            if (type == GameEntityTypes.Hunter)
            {
                position = GameConfig.InvaderSpawnPoint;
            }

            SpawnRequestSystem spawnRequestSystem = Worker.World.GetOrCreateSystem<SpawnRequestSystem>();
            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                Position = HelperFunctions.Vector3fFromUnityVector(position),
                TypeToSpawn = type
            }, OnCreatePlayerResponse, DTO.Converters.SerializeArguments(new DTO.PlayerConfig
            {
                position = HelperFunctions.Vector3fFromUnityVector(position),
                playerType = type
            }));
        }

        //Move this and the creation requests to manager and just have this call it from manager.
        private void OnCreatePlayerResponse(EntityId createdEntityId)
        {            
                if (PlayerRole == GameEntityTypes.Hunter)
                {
                    AddInvaderSystems();
                    SpawnRequestSystem spawnRequestSystem = Worker.World.GetExistingSystem<SpawnRequestSystem>();
                    var invaderUnitSpawnPoints = GameConfig.InvaderUnitSpawnPoints;
                    var amountOfPoints = invaderUnitSpawnPoints.Length;
                    for (int i = 0; i < amountOfPoints; ++i)
                    {
                        UnitConfig unitConfig = new UnitConfig
                        {
                            ownerId = createdEntityId.Id,
                            position = HelperFunctions.Vector3fFromUnityVector(invaderUnitSpawnPoints[i]),
                            unitType = MdgSchema.Units.UnitTypes.Worker
                        };
                        spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
                        {
                            Position = HelperFunctions.Vector3fFromUnityVector(invaderUnitSpawnPoints[i]),
                            Count = 1,
                            TypeToSpawn = GameEntityTypes.Unit
                        }, null, Converters.SerializeArguments(unitConfig));
                    }
                }
                else if (PlayerRole == GameEntityTypes.Hunted)
                {
                    // AddDefenderSystems (if any), maybe do do it on server side.
                }
                PlayerFinishedLoading = true;
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
            

            // Try removing all systems incase any of them are culrpit and see if positions synced.
            // if still no go, then look at monobehaviours and see what's up there. 

            Worker.World.GetOrCreateSystem<SpawnRequestSystem>();
            Worker.World.GetOrCreateSystem<PointRequestSystem>();
            Worker.World.GetOrCreateSystem<WeaponSystem>();

            // Invader systems.
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = false; 
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = false;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = false;

            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            ClientGameObjectCreator = new ClientGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType, GameConfig);
            ClientGameObjectCreator.OnEntityAdded += OnNewEntityAdded;
            ClientGameObjectCreator.OnGameObjectSpawned += OnLinkedGameObjectSpawned;
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, ClientGameObjectCreator);
        }

        private void OnLinkedGameObjectSpawned(GameObject obj)
        {
            if (obj.GetComponent<LinkedEntityComponent>() != null)
            {
                obj.SetActive(false);
                StartCoroutine(ActivateAfterSync(obj));
            }
        }

        IEnumerator ActivateAfterSync(GameObject spawned)
        {
            yield return new WaitForEndOfFrame();
            spawned.SetActive(true);
        }

        private void AddInvaderSystems()
        {
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<SelectionSystem>().Init(ClientGameObjectCreator, ClientGameObjectCreator.PlayerLink.EntityId);
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = true;
        //    Worker.World.GetOrCreateSystem<UnitRerouteSystem>().Enabled = true;
        }



        private void AddDefenderSystems()
        {
            // Remove invader systems.
            Worker.World.DestroySystem(Worker.World.GetExistingSystem<SelectionSystem>());
            Worker.World.DestroySystem(Worker.World.GetExistingSystem<CommandGiveSystem>());
            Worker.World.DestroySystem(Worker.World.GetExistingSystem<CommandUpdateSystem>());
            Worker.World.DestroySystem(Worker.World.GetExistingSystem<UnitRerouteSystem>());
        }

        public void CloseConnection()
        {
            Application.Quit();
        }
    }
}
