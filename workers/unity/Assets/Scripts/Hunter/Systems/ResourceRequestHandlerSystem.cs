using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using ResourceSchema = MdgSchema.Game.Resource;
namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ResourceRequestHandlerSystem : ComponentSystem
    {
        class ResourceRequestException : System.Exception
        {
            public long RequestId { private set; get; }
            public ResourceSchema.ResourceRequestType ResourceRequestType { private set; get; }
            public ResourceRequestException(long requestId, ResourceSchema.ResourceRequestType type, string message) : base(message)
            {
                RequestId = requestId;
                ResourceRequestType = type;
            }
        }
        WorkerSystem workerSystem;
        CommandSystem commandSystem;
        //temproary here need to figure oit best way to manage these.
        EntityId managerId = new EntityId(5);
        protected override void OnCreate()
        {
            base.OnCreate();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }
        protected override void OnUpdate()
        {
            try
            {
                ProcessRequests();
            }
            // But I don't want this ot interrupt other cycles
            // this was kind of waste of time, think about more later.
            catch( ResourceRequestException requestError)
            {
                switch (requestError.ResourceRequestType)
                {
                    case ResourceSchema.ResourceRequestType.OCCUPY:
                        commandSystem.SendResponse(new ResourceSchema.ResourceManager.Occupy.Response
                        {
                            RequestId = requestError.RequestId,
                            FailureMessage = requestError.Message
                        });
                        break;
                }
            }
            catch(System.Exception error)
            {
                Debug.LogError("Internal server error");
            }

        }
        private void ProcessRequests()
        {

            #region Process Release Requests
            var releaseReqeusts = commandSystem.GetRequests<ResourceSchema.ResourceManager.Release.ReceivedRequest>(managerId);
            for (int i = 0; i < releaseReqeusts.Count; ++i)
            {
                ref readonly var request = ref releaseReqeusts[i];

                // Get resource
                if (workerSystem.TryGetEntity(request.Payload.ResourceId, out Entity entity))
                {
                    if (!EntityManager.HasComponent<ResourceSchema.Occupied.Component>(entity))
                    {
                        commandSystem.SendResponse(new ResourceSchema.ResourceManager.Release.Response
                        {
                            RequestId = request.RequestId,
                            FailureMessage = $"{request.Payload.Occupant} is not an occupant of {request.Payload.ResourceId}"
                        });
                    }
                    // Get Occupied component.
                    var occupiedComponent = EntityManager.GetComponentData<ResourceSchema.Occupied.Component>(entity);
                    occupiedComponent.Occupants.Remove(request.Payload.Occupant);
                    // If no more occupants, remove occupied component.
                    if (occupiedComponent.Occupants.Count == 0)
                    {
                        PostUpdateCommands.RemoveComponent<ResourceSchema.Occupied.Component>(entity);
                    }
                    else
                    {
                        EntityManager.SetComponentData(entity, occupiedComponent);
                    }
                    commandSystem.SendResponse(new ResourceSchema.ResourceManager.Release.Response
                    {
                        RequestId = request.RequestId,
                        Payload = new ResourceSchema.ReleaseResponse()
                    });
                }
            }
            #endregion

            #region Process Occupy Requests
            var occupyRequests = commandSystem.GetRequests<ResourceSchema.ResourceManager.Occupy.ReceivedRequest>(managerId);
            for (int i = 0; i < occupyRequests.Count; ++i)
            {
                ref readonly var request = ref occupyRequests[i];

                // So need to get resource.
                if (workerSystem.TryGetEntity(request.Payload.ToOccupy, out Entity entity))
                {
                    List<EntityId> occupants;
                    if (EntityManager.HasComponent<ResourceSchema.Occupied.Component>(entity))
                    {
                        occupants = EntityManager.GetComponentData<ResourceSchema.Occupied.Component>(entity).Occupants;
                        ResourceSchema.Resource.Component resourceComponent = EntityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

                        if (occupants.Count > resourceComponent.MaximumOccupancy)
                        {
                            // Then response with maximum occupancy.
                            commandSystem.SendResponse<ResourceSchema.ResourceManager.Occupy.Response>(new ResourceSchema.ResourceManager.Occupy.Response
                            {
                                RequestId = request.RequestId,
                                Payload =  new ResourceSchema.OccupyResponse
                                {
                                    FullyOccupied = true,
                                    Occupants = occupants,
                                    ResourceId = request.Payload.ToOccupy
                                }
                            });
                            return;
                        }
                    }
                    else
                    {
                        occupants = new List<EntityId>();
                        PostUpdateCommands.AddComponent<ResourceSchema.Occupied.Component>(entity);
                    }
                    occupants.Add(request.Payload.Occupying);
                    PostUpdateCommands.SetComponent(entity, new ResourceSchema.Occupied.Component
                    {
                        Occupants = occupants
                    });
                }
            }
            #endregion


            // For now just added health to occupied component, that's fine for now, they don't need whole stat system.
            #region Process Collect Requests

            // This can't be jobified, because I can't have it be done in paralel due to race condition for collectors
            // collecting same resource. Jobify later, functional first, optimize later, won;t be huge change.
            var collectRequests = commandSystem.GetRequests<ResourceSchema.ResourceManager.Collect.ReceivedRequest>(managerId);

            for (int i = 0; i < collectRequests.Count; ++i)
            {
                // This 100% should be jobified could pass request as native array.
                ref readonly var request = ref collectRequests[i];
                if (workerSystem.TryGetEntity(request.Payload.ResourceId, out Entity entity))
                {
                    ResourceSchema.Occupied.Component occupiedComponent = EntityManager.GetChunkComponentData<ResourceSchema.Occupied.Component>(entity);
                    if (!occupiedComponent.Occupants.Contains(request.Payload.CollectorId))
                    {
                        // Should respond with not occupied response
                        commandSystem.SendResponse(new ResourceSchema.ResourceManager.Collect.Response
                        {
                            FailureMessage = $"The collect {request.Payload.CollectorId} is not currently occupying {request.Payload.ResourceId}",
                            RequestId = request.RequestId
                        });
                        continue;
                    }
                    int health = occupiedComponent.Health;
                    // Later on should minus the collect speed of occupant
                    health -= 1;
                    ResourceSchema.CollectResponse payload;
                    if (health == 0)
                    {
                        PostUpdateCommands.RemoveComponent<ResourceSchema.Occupied.Component>(entity);
                        payload = new ResourceSchema.CollectResponse
                        {
                            DepleterId = request.Payload.CollectorId,
                            ResourceId = request.Payload.ResourceId
                        };
                    }
                    else
                    {
                        payload = new ResourceSchema.CollectResponse
                        {
                            TimesUntilDepleted = health
                        };
                        //Otherwise if resource still exists update component
                        EntityManager.SetComponentData(entity, new ResourceSchema.Occupied.Component
                        {
                            Health = health,
                            Occupants = occupiedComponent.Occupants
                        });
                    }
                    commandSystem.SendResponse(new ResourceSchema.ResourceManager.Collect.Response {
                        RequestId = request.RequestId,
                        Payload = payload
                    }); 
                }
            }
            #endregion
        }
    }
}