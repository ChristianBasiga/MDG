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
using MDG.Hunter.Systems.UnitCreation;
using MDG.Hunter.Systems;
using MDG.Common.Systems;

namespace MDG
{

    // These are just examples.
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
            TransformSynchronizationHelper.AddServerSystems(Worker.World);
            //Worker.World.GetOrCreateSystem<CollisionDetectionSystem>();
            PlayerLifecycleHelper.AddServerSystems(Worker.World);
            //UnitCreationHelper.AddServerSystems(Worker.World);
            //UnitCreationHelper.AddClientSystems(Worker.World);
            //Create helper on entity for server and client systems
            //for current systems INit, just client.
        }

    }
}
