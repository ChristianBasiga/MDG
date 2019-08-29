using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using MDG.Common.Systems;
using MDG.Hunter.Systems;
using MDG.Hunter.Systems.UnitCreation;
using UnityEngine;

namespace MDG
{
    //Need to figure out spawnign stuff on server side and not.
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        public GameObject surface;
        public CustomGameObjectCreator customGameObjectCreator { get; private set; }
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

            PlayerLifecycleConfig.CreatePlayerEntityTemplate = Player.Templates.CreatePlayerEntityTemplate;

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
           // if (customGameObjectCreator == null)
           // {
                GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
                customGameObjectCreator = new CustomGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType);
                customGameObjectCreator.surface = surface;
                GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customGameObjectCreator);
            //}
            // So syncing works in dev scene but positions not in sync in other scene.

            
            TransformSynchronizationHelper.AddClientSystems(Worker.World);
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
            UnitCreationHelper.AddClientSystems(Worker.World);
            // This should actually be in server side, but later.
            Worker.World.GetOrCreateSystem<StatUpdateSystem>();
            Worker.World.GetOrCreateSystem<GameEntityInitSystem>();
            Worker.World.GetOrCreateSystem<MouseInputSystem>();
            Worker.World.GetOrCreateSystem<CommandGiveSystem>();
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>();
        }
    }
}
