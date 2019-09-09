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
using Unity.Rendering;
using Improbable;
using Unity.Entities;

namespace MDG
{
    //Need to figure out spawnign stuff on server side and not.
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";
        public CustomGameObjectCreator customGameObjectCreator { get; private set; }
        public Mesh mesh;
        public Material material;

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
            customGameObjectCreator = new CustomGameObjectCreator(defaultCreator, Worker.World, Worker.WorkerType);
            CustomGameObjectCreator.mesh = mesh;
            CustomGameObjectCreator.material = material;
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customGameObjectCreator);
            TransformSynchronizationHelper.AddClientSystems(Worker.World);
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
            UnitCreationHelper.AddClientSystems(Worker.World);
            // This should actually be in server side, but later.
            Worker.World.GetOrCreateSystem<StatUpdateSystem>();
            Worker.World.GetOrCreateSystem<GameEntityInitSystem>();
            Worker.World.GetOrCreateSystem<MoveSystem>();
            Worker.World.GetOrCreateSystem<MouseInputSystem>();
            Worker.World.GetOrCreateSystem<CommandGiveSystem>();
            Worker.World.GetOrCreateSystem<CommandUpdateSystem>();
            Worker.World.GetOrCreateSystem<EntitySyncSystem>();
            //WHY WONT YOU RENDER.
            /*
            Worker.World.GetOrCreateSystem<Unity.Rendering.RenderMeshSystemV2>();
            Worker.World.GetOrCreateSystem<Unity.Rendering.RenderBoundsUpdateSystem>();
            Worker.World.GetOrCreateSystem<Unity.Rendering.LodRequirementsUpdateSystem>();
            Worker.World.GetOrCreateSystem<Unity.Rendering.LightSystem>();*/
        }

        
    }
}
