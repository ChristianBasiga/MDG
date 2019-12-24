using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using MDG.Common;
using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using MDG.Common.MonoBehaviours.Synchronizers;
using MDG.Common.Systems.Point;
using MDG.Common.Systems.Spawn;
using MDG.Common.Systems.Weapon;
using MDG.DTO;
using MDG.Game.Util.Pool;
using MDG.Invader.Systems;
using MDG.ScriptableObjects.Game;
using MDG.Templates;
using MdgSchema.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSchema = MdgSchema.Game;

namespace MDG
{
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        public GameEntityTypes PlayerRole { private set; get; }
        public Dictionary<string, GameObject> LoadedUI { private set; get; }
        public ClientGameObjectCreator ClientGameObjectCreator { get; private set; }
        public GameConfig GameConfig { private set; get; }

        public SpatialOSEntity GameManagerEntity { private set; get; }

        public List<SpatialOSEntity> TerritoryEntities { private set; get; }

        public bool PlayerJoiningRoom { get { return playerJoinRequestId.HasValue; } }
        public bool PlayerFinishedLoading { private set; get; }
        private long? playerJoinRequestId = null;
        CommandSystem commandSystem;
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
                    GameStatusSynchronizer gameStatusSynchronizer = gameObject.AddComponent<GameStatusSynchronizer>();
                    gameStatusSynchronizer.OnEndGame += GameStatusSynchronizer_OnEndGame;
                    GameManagerEntity = spatialOSEntity;
                    break;
                case "Territory":
                    TerritoryEntities.Add(spatialOSEntity);
                    break;
            }
        }

        private void GameStatusSynchronizer_OnEndGame()
        {
            if (LoadedUI != null)
            {
                var keys = LoadedUI.Keys;
                foreach (string key in keys)
                {
                    Destroy(LoadedUI[key]);
                }
                LoadedUI.Clear();
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
                    },null, Converters.SerializeArguments(unitConfig));
                }
            }
            else if (PlayerRole == GameEntityTypes.Hunted)
            {
                StartCoroutine(LoadDefenderUI());
            }

            playerJoinRequestId = commandSystem.SendCommand(new GameSchema.GameStatus.JoinGame.Request
            {
                Payload = new GameSchema.PlayerJoinRequest
                {
                    PlayerRole = PlayerRole,
                    EntityId = createdEntityId
                },
                TargetEntityId = GameManagerEntity.SpatialOSEntityId
            });
            Debug.Log("sent join game request " + playerJoinRequestId);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);

            commandSystem = Worker.World.GetExistingSystem<CommandSystem>();

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
            ClientGameObjectCreator = new ClientGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType, GetComponent<PoolManager>());
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
                if (obj.TryGetComponent(out IPlayerSynchronizer playerSynchronizer))
                {
                    StartCoroutine(LinkPlayerToWorker(playerSynchronizer));
                }
            }
        }


        IEnumerator LinkPlayerToWorker(IPlayerSynchronizer playerSynchronizer)
        {
            yield return new WaitUntil(() => GameManagerEntity.SpatialOSEntityId.IsValid());
            playerSynchronizer.LinkClientWorker(this);
        }
        IEnumerator ActivateAfterSync(GameObject spawned)
        {
            yield return new WaitForEndOfFrame();
            spawned.SetActive(true);
        }

        private void AddInvaderSystems()
        {
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<SelectionSystem>().Init(ClientGameObjectCreator);
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


        private IEnumerator LoadDefenderUI()
        {
            object[] defenderUI = Resources.LoadAll("UserInterface/DefenderUI/");
            LoadedUI = new Dictionary<string, GameObject>();
            for (int i = 0; i < defenderUI.Length; ++i)
            {
                GameObject gameObject = defenderUI[i] as GameObject;
                // Better to load in each time.
                GameObject instance = Instantiate(gameObject);
                Debug.Log("gameObject name " + gameObject.name);
                LoadedUI.Add(gameObject.name, instance);
                yield return new WaitForEndOfFrame();
            }
        }


        public void CloseConnection()
        {
            commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
            {
                EntityId = ClientGameObjectCreator.PlayerLink.EntityId
            });
            Application.Quit();
        }

        private void Update()
        {
            if (playerJoinRequestId.HasValue)
            {
                Debug.Log("checking responses");
                var responses = commandSystem.GetResponse<GameSchema.GameStatus.JoinGame.ReceivedResponse>(playerJoinRequestId.Value);
                Debug.Log($"Got {responses.Count} responses");
                if (responses.Count > 0)
                {
                    switch (responses[0].StatusCode)
                    {
                        case StatusCode.Success:
                            playerJoinRequestId = null;
                            PlayerFinishedLoading = true;
                            break;
                        default:
                            Debug.LogError(responses[0].Message);
                            Debug.LogError("Failed to join the game");
                            break;
                    }
                }
            }
        }
    }
}
