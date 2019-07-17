using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;
using Player.Metadata;
namespace MDG
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        public const string WorkerType = "UnityGameLogic";
        [Require] private PositionWriter positionWriter;
        [Require] private TransformInternalReader internalReader;
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

            IEntityGameObjectCreator defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);
            CustomObjectCreation customCreator = new CustomObjectCreation(defaultCreator, Worker.World, Worker.WorkerType);

            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customCreator);
            TransformSynchronizationHelper.AddServerSystems(Worker.World);

            PlayerLifecycleHelper.AddServerSystems(Worker.World);
        }

    
        //Keep this the same , but just pass in stuff to playerCreationArguments to specify kind of player.
        //Adding to snapshot would be entities that start off in game, not players that only come upon connection.
        //And request player creation manually, setting atomatic to flase,
        private static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] playerCreationArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = WorkerType;

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            //template.AddComponent(new PlayerMetaData.Snapshot(playerType), clientAttribute);
            template.SetReadAccess(serverAttribute);
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, serverAttribute);
            

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }
    }
}
