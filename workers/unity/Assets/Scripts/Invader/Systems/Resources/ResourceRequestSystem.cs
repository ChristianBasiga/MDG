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
using MDG.Common.Components;
using System;

namespace MDG.Invader.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
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
            public Action<ResourceRequestReponse> callback;
        }

        public struct ResourceRequestReponse
        {
            public bool Success;
            public string Message;
            public EntityId EffectedResource;
            public CollectResponse? CollectResponse;
            public OccupyResponse? OccupyResponse;
            public ReleaseResponse? ReleaseResponse;
        }



        private CommandSystem commandSystem;
        private WorkerSystem workerSystem;
        private long resourceManagerRequestId;
        private Queue<ResourceRequestHeader> pendingRequests;
        private Dictionary<ResourceRequestType, Dictionary<long, ResourceRequestHeader>> reqIdsToPayload;

        protected override void OnCreate()
        {
            base.OnCreate();
            reqIdsToPayload = new Dictionary<ResourceRequestType, Dictionary<long, ResourceRequestHeader>>();
            pendingRequests = new Queue<ResourceRequestHeader>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }

        // Maybe break this in parts to have dedicated responses for each.
        // Add callback, makes it easier for testing. Callbacks may be separated for each kind of request.
        // that would make it better.
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
               
                //EntityId resourceManagerEntityId = resourceManagerIds[UnityEngine.Random.Range(0, resourceManagerIds.Count)];
                // For each pending request choose random resource manager worker instance.
                switch (resourceRequestHeader.ResourceRequestType)
                {
                    case ResourceRequestType.RELEASE:
                        ReleaseRequest releasePayload = new ReleaseRequest
                        {
                            Occupant = resourceRequestHeader.OccupantId,
                            ResourceId = resourceRequestHeader.ResourceId
                        };
                        requestId = commandSystem.SendCommand(new Resource.Release.Request
                        {
                            TargetEntityId = resourceRequestHeader.ResourceId,
                            Payload = releasePayload
                        });
                        break;
                    case ResourceRequestType.COLLECT:
                        CollectRequest collectPayload = new CollectRequest
                        {
                            CollectorId = resourceRequestHeader.OccupantId,
                            ResourceId = resourceRequestHeader.ResourceId
                        };
                        // Send request to a resource manager worker.
                        requestId = commandSystem.SendCommand<Resource.Collect.Request>(new Resource.Collect.Request
                        {
                            TargetEntityId = resourceRequestHeader.ResourceId,
                            Payload = collectPayload
                        });
                        break;
                    case ResourceRequestType.OCCUPY:
                        OccupyRequest occupyPayload = new OccupyRequest
                        {
                            Occupying = resourceRequestHeader.OccupantId,
                            ToOccupy = resourceRequestHeader.ResourceId,
                        };

                        requestId = commandSystem.SendCommand(new Resource.Occupy.Request
                        {
                            TargetEntityId = resourceRequestHeader.ResourceId,
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
                    if (!reqIdsToPayload.TryGetValue(resourceRequestHeader.ResourceRequestType, out _))
                    {
                        reqIdsToPayload[resourceRequestHeader.ResourceRequestType] = new Dictionary<long, ResourceRequestHeader>();
                    }
                    reqIdsToPayload[resourceRequestHeader.ResourceRequestType][requestId] = resourceRequestHeader;
                }
            }
            #endregion
            // Todo: Set up system for repated reqeusts like I did for inventory but perhaps more general?
            #region Process all responses
            // Only if we've sent any requests that haven't yet been resolved.
            if (reqIdsToPayload.Count > 0)
            {
                // No bueno, I should do this based on type instead.
                Dictionary<long, ResourceRequestHeader> reqIds;
                if (reqIdsToPayload.TryGetValue(ResourceRequestType.COLLECT, out reqIds))
                {
                    if (reqIds.Count > 0)
                    {
                        var responses = commandSystem.GetResponses<Resource.Collect.ReceivedResponse>();
                        for (int i = 0; i < responses.Count; ++i)
                        {
                            ref readonly var response = ref responses[i];
                            if (reqIds.TryGetValue(response.RequestId, out ResourceRequestHeader resourceRequestHeader))
                            {
                                ResourceRequestReponse resourceRequestReponse = new ResourceRequestReponse
                                {
                                    Message = response.Message,
                                    Success = response.StatusCode == StatusCode.Success,
                                    EffectedResource = response.ResponsePayload.GetValueOrDefault().ResourceId,
                                    CollectResponse = response.ResponsePayload
                                };
                                switch (response.StatusCode)
                                {
                                    case StatusCode.Success:
                                        if (response.ResponsePayload.HasValue && workerSystem.TryGetEntity(response.RequestPayload.CollectorId, out Unity.Entities.Entity entity))
                                        {
                                            /* No need anymore.
                                            PostUpdateCommands.AddComponent(entity, new PendingInventoryAddition
                                            {
                                                ItemId = 1,
                                                Count = 1
                                            });*/
                                        }
                                        break;
                                }
                                resourceRequestHeader.callback?.Invoke(resourceRequestReponse);
                            }
                        }
                    }
                }
                if (reqIdsToPayload.TryGetValue(ResourceRequestType.OCCUPY, out reqIds))
                {
                    if (reqIds.Count > 0)
                    {
                        var responses = commandSystem.GetResponses<Resource.Occupy.ReceivedResponse>();
                        for (int i = 0; i < responses.Count; ++i)
                        {
                            ref readonly var response = ref responses[i];
                            if (reqIds.TryGetValue(response.RequestId, out ResourceRequestHeader resourceRequestHeader))
                            {
                                ResourceRequestReponse resourceRequestReponse = new ResourceRequestReponse
                                {
                                    Message = response.Message,
                                    Success = response.StatusCode == StatusCode.Success,
                                    EffectedResource = response.RequestPayload.ToOccupy,
                                    OccupyResponse = response.ResponsePayload.Value
                                };
                                //There's going to be alot of repetition with checking for status codes here.
                                // Hmm. Fuck it for now. POC this, then improve later, I waste time pre-optimizing too much over little things.
                                switch (response.StatusCode)
                                {
                                    case StatusCode.Success:
                                        // Occupy request acknowledged process response.
                                        if (response.ResponsePayload.Value.FullyOccupied && !response.ResponsePayload.Value.Occupied)
                                        {
                                            resourceRequestReponse.Success = false;
                                            UnityEngine.Debug.LogError($"Resource {response.RequestPayload.ToOccupy} is fully occupied");
                                        }
                                        break;
                                    default:

                                        break;
                                }
                                resourceRequestHeader.callback?.Invoke(resourceRequestReponse);
                            }
                        }
                    }
                }
                if (reqIdsToPayload.TryGetValue(ResourceRequestType.RELEASE, out reqIds))
                {
                    if (reqIds.Count > 0)
                    {
                        var responses = commandSystem.GetResponses<Resource.Release.ReceivedResponse>();
                        for (int i = 0; i < responses.Count; ++i)
                        {
                            ref readonly var response = ref responses[i];

                            if (reqIds.TryGetValue(response.RequestId, out ResourceRequestHeader resourceRequestHeader))
                            {
                                ResourceRequestReponse resourceRequestReponse = new ResourceRequestReponse
                                {
                                    Message = response.Message,
                                    Success = response.StatusCode == StatusCode.Success,
                                    EffectedResource = response.RequestPayload.ResourceId,
                                    ReleaseResponse = response.ResponsePayload
                                };

                                switch (response.StatusCode)
                                {
                                    case StatusCode.Success:
                                        // Update UI.
                                        break;
                                    default:
                                        break;
                                }
                                resourceRequestHeader.callback?.Invoke(resourceRequestReponse);
                            }
                        }
                    }
                }
                #endregion
            }

        }
    }
}