using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace MDG
{
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
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
            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            CustomObjectCreation customCreator = new CustomObjectCreation(defaultCreator, Worker.World, Worker.WorkerType);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customCreator );
            TransformSynchronizationHelper.AddClientSystems(Worker.World);
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
        }
    }
}
