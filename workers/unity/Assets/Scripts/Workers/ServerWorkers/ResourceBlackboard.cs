using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Game.Resource;
using Improbable.Worker.CInterop;

namespace MDG
{

    // Rename to just blackboard, and it will have multiple dictionaries and reference to each player.
    // Each interactable will have entityId, and will pass in id to get actual data.
    // In request. The client Blackboard will do that.
    public class ResourceBlackboard: WorkerConnector
    {
        //FOr now instead of amking different kind of workers, just diff monohbehaviours on the worker.
        public const string WorkerType = "Blackboard";
        // Can only require if this worker as write access.
        [Require] ResourceCommandReceiver resourceCommandReceiver;
        [Require] ResourceWriter resourceWriter;
        
        //Could be nested dictionary
        //Here need to have entitty class that would map to there, for now string is fine.
        //Value of this will be injected through zenject.
        Dictionary<string, Resource> resourceState;


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

            resourceCommandReceiver.OnOccupyRequestReceived += HandleResourceRequest;
        }

        private void HandleResourceRequest(Resource.Occupy.ReceivedRequest req)
        {

        }

    }
}