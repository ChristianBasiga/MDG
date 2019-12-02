using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using MDG.Common.Systems;
using MDG.Invader.Systems;
using UnityEngine;
using Unity.Rendering;
using Improbable;
using Unity.Entities;
using MDG.Templates;
using MDG.Common.Systems.Inventory;
using MDG.Common.Systems.Spawn;
using MDG.Common.Systems.Point;
using MDG.Common.Systems.Weapon;
using MDG.Common.Systems.Structure;
using MDG.ScriptableObjects.Game;
namespace MDG
{
    //Need to figure out spawnign stuff on server side and not.
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        public ClientGameObjectCreator clientGameObjectCreator { get; private set; }
        GameConfig gameConfig;
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
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    case ConnectionService.AlphaLocator:
                        builder.SetConnectionFlow(new AlphaLocatorFlow(initializer));
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
            gameConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseGameConfig") as GameConfig;
            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            TransformSynchronizationHelper.AddClientSystems(Worker.World);
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
            Worker.World.GetOrCreateSystem<SpawnRequestSystem>();
            Worker.World.GetOrCreateSystem<InventoryRequestSystem>();
            Worker.World.GetOrCreateSystem<PointRequestSystem>();
            Worker.World.GetOrCreateSystem<WeaponSystem>();
            //Invader systems.

            /*
           

            */
            Worker.World.GetOrCreateSystem<ResourceRequestSystem>();
            Worker.World.GetOrCreateSystem<ResourceMonitorSystem>();
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = false; 
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = false;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = false;
            Worker.World.GetOrCreateSystem<UnitRerouteSystem>().Enabled = false;
            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            clientGameObjectCreator = new ClientGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType, gameConfig);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, clientGameObjectCreator);
        }


        public void AddInvaderSystems()
        {
            Worker.World.GetOrCreateSystem<SelectionSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<CommandGiveSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>().Enabled = true;
            Worker.World.GetOrCreateSystem<UnitRerouteSystem>().Enabled = true;
        }

        public void CloseConnection()
        {
            // Other clean up and logs...
            Application.Quit();
        }
    }
}
