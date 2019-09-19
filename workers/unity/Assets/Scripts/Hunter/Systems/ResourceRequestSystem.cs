using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Improbable.Worker.CInterop.Query;
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using MdgSchema.Game.Resource;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using Improbable.Gdk.Core.Commands;
using System.Linq;

namespace MDG.Hunter.Systems
{
    /// <summary>
    /// For sending any requests that have to do with resource manager and updating resources on server side.
    /// </summary>
    /// Todo: Rename this file.
    /// 
    [DisableAutoCreation]
    public class ResourceRequestSystem : ComponentSystem
    {
        // Would have to have events for each request
        // basically acting as responses.
        public delegate void CollectResponseEventHandler(CollectResponse receivedResponse);
        public event CollectResponseEventHandler OnCollect;
        // Need to have generic payload too.

        public struct ResourceRequestHeader
        {
            //Occupant id not really super true, but makes enough sense.
            public EntityId OccupantId;
            public EntityId ResourceId;
            public ResourceRequestType ResourceRequestType;
        }

        private CommandSystem commandSystem;
        private List<EntityId> resourceManagerIds;
        private long resourceManagerRequestId;
        private Queue<ResourceRequestHeader> pendingRequests;
        private Dictionary<ResourceRequestType, Dictionary<long,ResourceRequestHeader>> reqIdsToPayload;
        private readonly EntityQuery resourceManagerGroup = new EntityQuery
        {
            Constraint = new ComponentConstraint(ResourceManager.ComponentId),
            ResultType = new SnapshotResultType()
        };

        protected override void OnCreate()
        {
            base.OnCreate();
            reqIdsToPayload = new Dictionary<ResourceRequestType, Dictionary<long, ResourceRequestHeader>>();
            pendingRequests = new Queue<ResourceRequestHeader>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            resourceManagerIds = new List<EntityId>();
        }

        // For lazy loading them
        private void GetResourceManagerWorkers()
        {
            resourceManagerRequestId = commandSystem.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = resourceManagerGroup
            });
        }

        private void SetResourceManagerWorkers()
        {
            // This system is run on each client, so impossible to get responses more than jsut this requstId
            // so no need to check.
            var receivedResponses = commandSystem.GetResponse<WorldCommands.EntityQuery.ReceivedResponse>(resourceManagerRequestId);
            for (int i = 0; i < receivedResponses.Count; ++i)
            {
                ref readonly var response = ref receivedResponses[i];
                switch (response.StatusCode)
                {
                    case StatusCode.Success:
                        resourceManagerIds.AddRange(response.Result.Keys);
                        break;
                    default:
                        UnityEngine.Debug.LogError($"Failed to get resource manager workers {response.Message}");
                        break;
                }
            }
        }

     
        public void SendRequest(ResourceRequestHeader resourceRequestHeader)
        {
            pendingRequests.Enqueue(resourceRequestHeader);
        }


        protected override void OnUpdate()
        {


            #region SendPending Requests to respective worker
            while (pendingRequests.Count > 0)
            {
                ResourceRequestHeader resourceRequestHeader = pendingRequests.Dequeue();
                long requestId = -1;
                EntityId resourceManagerEntityId = resourceManagerIds[UnityEngine.Random.Range(0, resourceManagerIds.Count)];
                // For each pending request choose random resource manager worker instance.

                switch (resourceRequestHeader.ResourceRequestType)
                {
                    // For the most part all of the payloads are damn near identitical.
                    // I mean more scalable this way if decide want more in payload so fine like this for now.
                    // Main this is their responses are different.
                    // I might have to run my own system for parralelizing that process
                    // actually no, there should be order. Release, Occupy, Collect.
                    // this way Occupying requests get access to resource as soon as freed
                    // instead of frame later, which isn't big deal, but makes sense.
                    case ResourceRequestType.COLLECT:
                        CollectRequest collectPayload = new CollectRequest
                        {
                            CollectorId = resourceRequestHeader.OccupantId,
                            ResourceId = resourceRequestHeader.ResourceId
                        };
                        // Send request to a resource manager worker.
                        requestId = commandSystem.SendCommand<ResourceManager.Collect.Request>(new ResourceManager.Collect.Request
                        {
                            TargetEntityId = resourceManagerEntityId,
                            Payload = collectPayload
                        });
                        break;
                    case ResourceRequestType.OCCUPY:
                        OccupyRequest occupyPayload = new OccupyRequest
                        {
                            Occupying = resourceRequestHeader.OccupantId,
                            ToOccupy = resourceRequestHeader.ResourceId,
                        };
                        requestId = commandSystem.SendCommand(new ResourceManager.Occupy.Request
                        {
                            TargetEntityId = resourceManagerEntityId,
                            Payload = occupyPayload
                        });
                        break;
                      // Will do release later, should confirm that what ahve so far actually works.
                }
                if (requestId != -1)
                {
                    // Storing both as if response we get returns with failed status code
                    // we can then resend request with same header to try again
                    // if it was time out for example. Less dupe code with contain key but tryGet is faster.
                    if (reqIdsToPayload.TryGetValue(resourceRequestHeader.ResourceRequestType, out Dictionary<long, ResourceRequestHeader> reqIds))
                    {
                        reqIds[requestId] = resourceRequestHeader;
                    }
                    else
                    {
                        reqIdsToPayload[resourceRequestHeader.ResourceRequestType] = new Dictionary<long, ResourceRequestHeader>();
                        reqIdsToPayload[resourceRequestHeader.ResourceRequestType][requestId] = resourceRequestHeader;
                    }
                }
            }
            #endregion
            #region Process all responses
            // Only if we've sent any requests that haven't yet been resolved.
            if (reqIdsToPayload.Count > 0)
            {
                if (reqIdsToPayload.TryGetValue(ResourceRequestType.COLLECT, out Dictionary<long, ResourceRequestHeader> reqIds))
                {
                    if (reqIds.Count > 0)
                    {
                        var requests = commandSystem.GetResponses<ResourceManager.Collect.ReceivedResponse>();
                        for( int i = 0; i < requests.Count; ++i)
                        {
                            ref readonly var request = ref requests[i];

                            //There's going to be alot of repetition with checking for status codes here.
                            // Hmm. Fuck it for now. POC this, then improve later, I waste time pre-optimizing too much over little things.
                            switch (request.StatusCode)
                            {
                                case StatusCode.Success:
                                    // So here's what should happen. Query response and see if time left is 0.
                                    // if so. Then add it to inventory of Unit. Now
                                    // I could have a dictionary of depleted resources in this frame.
                                    // a native dictionary.
                                    // Then run a job in parallel reading from dictionary to either add to inventory or ignore
                                    // and simply remove CollectCommand component from entity.
                                    // But it shouldn't be in this system. 
                                    // Maybe an event, non spatial one that CommandUpdate can subscribe to. 
                                    // then PostUpdateCommands resolve in main thread but can be called from non main thread.
                                    // payload of event must basically be the response.
                                    // I can't do anything with payload alone. Maybe maintain a native list of payloads.

                                    // Trigger event that collected.
                                    if (request.ResponsePayload.HasValue) {
                                        OnCollect?.Invoke(request.ResponsePayload.Value);
                                     }
                                    break;
                            }
                        }
                    }
                }
            }
            #endregion
        }

    }
}