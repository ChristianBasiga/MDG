using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;
using MdgSchema.Player;
using MdgSchema.Lobby;
namespace MDG
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        public const string WorkerType = "UnityGameLogic";
        private async void Start()
        {

            IConnectionFlow flow;
            ConnectionParameters connectionParameters;


            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType));
                connectionParameters = CreateConnectionParameters(WorkerType);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerType,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worker.World.GetOrCreateSystem<MetricSendSystem>();

            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);

            CustomObjectCreation customCreator = new CustomObjectCreation(defaultCreator, Worker.World, Worker.WorkerType);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customCreator);
            TransformSynchronizationHelper.AddServerSystems(Worker.World);
            PlayerLifecycleHelper.AddServerSystems(Worker.World);
        }
                
    }
}
