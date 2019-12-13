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

namespace MDG
{
    //Need to figure out spawnign stuff on server side and not.
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        GameEntityTypes playerRole;
        public ClientGameObjectCreator ClientGameObjectCreator { get; private set; }
        public GameConfig GameConfig { private set; get; }

        public SpatialOSEntity GameManagerEntity { private set; get; }

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

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }


        private void OnNewEntityAdded(SpatialOSEntity spatialOSEntity)
        {

            if (spatialOSEntity.HasComponent<GameSchema.GameStatus.Component>())
            {
                Debug.Log("Game Manager Spawned");
                GameManagerEntity = spatialOSEntity;
            }
        }

        private void SpawnPlayer(MdgSchema.Common.GameEntityTypes type)
        {
            Vector3 position = GameConfig.DefenderSpawnPoints[0];
            playerRole = type;
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
                if (playerRole == GameEntityTypes.Hunter)
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
                else if (playerRole == GameEntityTypes.Hunted)
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
           // Worker.World.GetOrCreateSystem<InventoryRequestSystem>();
           
            Worker.World.GetOrCreateSystem<PointRequestSystem>();
            Worker.World.GetOrCreateSystem<WeaponSystem>();
            // Worker.World.GetOrCreateSystem<ResourceRequestSystem>();
            // Worker.World.GetOrCreateSystem<ResourceMonitorSystem>();
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = false; 
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = false;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = false;
            Worker.World.GetOrCreateSystem<UnitRerouteSystem>().Enabled = false;
            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            ClientGameObjectCreator = new ClientGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType, GameConfig);
           // ClientGameObjectCreator.OnGameObjectSpawned += OnLinkedGameObjectSpawned;
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, ClientGameObjectCreator);
        }

        private void OnLinkedGameObjectSpawned(GameObject obj)
        {
            gameObject.SetActive(false);
            StartCoroutine(ActivateAfterSync(obj));
        }

        IEnumerator ActivateAfterSync(GameObject gameObject)
        {
            yield return new WaitForEndOfFrame();
            gameObject.SetActive(true);
        }

        public void AddInvaderSystems()
        {
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<SelectionSystem>().Init(ClientGameObjectCreator);
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<UnitRerouteSystem>().Enabled = true;
        }

        public void CloseConnection()
        {
            Application.Quit();
        }


        private void Update()
        {
            if (GameManagerEntity.SpatialOSEntityId.IsValid())
            {
                GameSchema.GameStatus.Component component = GameManagerEntity.GetComponent<GameSchema.GameStatus.Component>();
                if (component.Started)
                {
                    mainOverlayHUD.UpdateTime(component.TimeLeft);
                }
            }
        }
    }
}
