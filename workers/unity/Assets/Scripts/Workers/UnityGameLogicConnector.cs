using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;
using Mdg.Player.Metadata;

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

            GameObjectCreatorFromMetadata defaultCreator = new GameObjectCreatorFromMetadata(Worker.WorkerType, Worker.Origin, Worker.LogDispatcher);

            CustomObjectCreation customCreator = new CustomObjectCreation(defaultCreator, Worker.World, Worker.WorkerType);

            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customCreator);
           // GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World);

            TransformSynchronizationHelper.AddServerSystems(Worker.World);


            PlayerLifecycleHelper.AddServerSystems(Worker.World);

            //So when worker connection established, create player.


            // When this connection is established it needs to come with info on which one the player is.
            // Fpr testomg spawn both types.
            //So it failes because of this.
           




        }

        //Move this and the creation requests to manager and just have this call it from manager.
        private void OnCreatePlayerResponse(PlayerCreator.CreatePlayer.ReceivedResponse response)
        {
            if (response.StatusCode != StatusCode.Success)
            {
                Debug.LogWarning($"Error: {response.Message}");
            }
        }




        //Move this to templates file.
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] playerCreationArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = WorkerType;

            //Deserializate playerCreationArguments.
            DTO.PlayerConfig creationArgs = DTO.Converters.DeserializeArguments<DTO.PlayerConfig>(playerCreationArguments);

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new Mdg.Player.PlayerTransform.Snapshot(), clientAttribute);
            template.AddComponent(new PlayerMetaData.Snapshot(creationArgs.playerType), clientAttribute);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);
            return template;
        }
    }
}
