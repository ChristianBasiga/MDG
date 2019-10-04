using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using ResourceSchema = MdgSchema.Game.Resource;
using System;

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

        struct ResourceUpdatePayload
        {
            public Dictionary<EntityId, List<Tuple<long,EntityId>>> occupying;
            public Dictionary<EntityId, List<Tuple<long, EntityId>>> collecting;
            public Dictionary<EntityId, List<Tuple<long, EntityId>>> releasing;
        }
        Dictionary<EntityId, ResourceUpdatePayload> resourceIdToUpdate;
        WorkerSystem workerSystem;
        CommandSystem commandSystem;
        EntityQuery resourceGroup;
        protected override void OnCreate()
        {
            base.OnCreate();
            resourceIdToUpdate = new Dictionary<EntityId, ResourceUpdatePayload>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            resourceGroup = GetEntityQuery(
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<ResourceSchema.ResourceMetadata.Component>(),
                ComponentType.ReadWrite<ResourceSchema.Resource.Component>(),
                ComponentType.ReadOnly<ResourceSchema.Resource.ComponentAuthority>()
                );

            resourceGroup.SetFilter(ResourceSchema.Resource.ComponentAuthority.Authoritative);
        }

        // Super requires change, cause occupied component is spatialOS and can't be added later.
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
                        commandSystem.SendResponse(new ResourceSchema.Resource.Occupy.Response
                        {
                            RequestId = requestError.RequestId,
                            FailureMessage = requestError.Message
                        });
                        break;
                }
            }
            catch(System.Exception error)
            {
                Debug.LogError("Internal server error: " + error.Message);
            }

        }

        // This needs change.
        // Filter on authority of acl.
        // Filter on authority on Occupied.


        // If this works, it works. Refactor this later to be more streamlined 
        private void ProcessRequests()
        {
            if (resourceGroup.CalculateEntityCount() == 0) return;
            ResourceUpdatePayload resourceUpdatePayload = new ResourceUpdatePayload
            {
               occupying = new Dictionary<EntityId, List<Tuple<long, EntityId>>>(),
               collecting = new Dictionary<EntityId, List<Tuple<long, EntityId>>>(),
               releasing = new Dictionary<EntityId, List<Tuple<long, EntityId>>>()
            };
               
            #region Process Release Requests
            var releaseReqeusts = commandSystem.GetRequests<ResourceSchema.Resource.Release.ReceivedRequest>();
            for (int i = 0; i < releaseReqeusts.Count; ++i)
            {
                ref readonly var request = ref releaseReqeusts[i];

                var releasing = resourceUpdatePayload.releasing;
                if (!releasing.TryGetValue(request.EntityId, out _))
                {
                    releasing[request.EntityId] = new List<Tuple<long, EntityId>>();
                }
                releasing[request.EntityId].Add(new Tuple<long, EntityId>(request.RequestId, request.Payload.Occupant));
                /*
                if (workerSystem.TryGetEntity(request.EntityId, out Entity entity))
                {
                    if (!EntityManager.HasComponent<ResourceSchema.Occupied.Component>(entity))
                    {
                        commandSystem.SendResponse(new ResourceSchema.Resource.Release.Response
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
                    // Biggest thing is sending response. Actually I CAN do that.
                    commandSystem.SendResponse(new ResourceSchema.Resource.Release.Response
                    {
                        RequestId = request.RequestId,
                        Payload = new ResourceSchema.ReleaseResponse()
                    });
                    }
                    */
            }
            #endregion

            #region Process Occupy Requests
            var occupyRequests = commandSystem.GetRequests<ResourceSchema.Resource.Occupy.ReceivedRequest>();
            // Maybe should instead do a for each.
            for (int i = 0; i < occupyRequests.Count; ++i)
            {
                ref readonly var request = ref occupyRequests[i];

                var occupying = resourceUpdatePayload.occupying;
                if (!occupying.TryGetValue(request.EntityId, out _))
                {
                    occupying[request.EntityId] = new List<Tuple<long, EntityId>>();
                }
                occupying[request.EntityId].Add(new Tuple<long, EntityId>(request.RequestId, request.Payload.Occupying));
                // So need to get resource.
                /*
                if (workerSystem.TryGetEntity(request.EntityId, out Entity entity))
                {
                    List<EntityId> occupants;
                    if (EntityManager.HasComponent<ResourceSchema.Occupied.Component>(entity))
                    {
                        occupants = EntityManager.GetComponentData<ResourceSchema.Occupied.Component>(entity).Occupants;
                        ResourceSchema.Resource.Component resourceComponent = EntityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

                        if (occupants.Count > resourceComponent.MaximumOccupancy)
                        {
                            // Then response with maximum occupancy.
                            commandSystem.SendResponse<ResourceSchema.Resource.Occupy.Response>(new ResourceSchema.Resource.Occupy.Response
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
                    
                }*/
            }
            #endregion


            // For now just added health to occupied component, that's fine for now, they don't need whole stat system.
            #region Process Collect Requests

            // This can't be jobified, because I can't have it be done in paralel due to race condition for collectors
            // collecting same resource. Jobify later, functional first, optimize later, won;t be huge change.
            var collectRequests = commandSystem.GetRequests<ResourceSchema.Resource.Collect.ReceivedRequest>();

            for (int i = 0; i < collectRequests.Count; ++i)
            {
                // This 100% should be jobified could pass request as native array.
                ref readonly var request = ref collectRequests[i];
                var collecting = resourceUpdatePayload.collecting;
                if (!collecting.TryGetValue(request.EntityId, out _))
                {
                    collecting[request.EntityId] = new List<Tuple<long, EntityId>>();
                }
                collecting[request.EntityId].Add(new Tuple<long, EntityId>(request.RequestId, request.Payload.CollectorId));
                /*
                if (workerSystem.TryGetEntity(request.EntityId, out Entity entity))
                {
                    ResourceSchema.Occupied.Component occupiedComponent = EntityManager.GetChunkComponentData<ResourceSchema.Occupied.Component>(entity);
                    if (!occupiedComponent.Occupants.Contains(request.Payload.CollectorId))
                    {
                        // Should respond with not occupied response
                        commandSystem.SendResponse(new ResourceSchema.Resource.Collect.Response
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
                        
                        // Along with deleting Occupied Component, I also need to add RespawnPending Component.
                        // spawns same spot with exact same static information, so really could just activate / deactivate game object.
                        // but that will be once SpawnSystem is set.
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
                    commandSystem.SendResponse(new ResourceSchema.Resource.Collect.Response {
                        RequestId = request.RequestId,
                        Payload = payload
                    }); 
                    
                }*/
            }
            #endregion
            // Jobify this down line maybe.
            Entities.With(resourceGroup).ForEach((ref SpatialEntityId spatialEntityId, ref ResourceSchema.ResourceMetadata.Component resourceMetadata, ref ResourceSchema.Resource.Component resource) =>
            {
                var releasing = resourceUpdatePayload.releasing;
                var collecting = resourceUpdatePayload.collecting;
                var occupying = resourceUpdatePayload.occupying;

                // Should be release, collect, occupy. Because collect may deplete the resource which would interrupt any future occupies.
                List<EntityId> occupants = resource.Occupants;
                int resourceHealth = resource.Health;

                if (releasing.TryGetValue(spatialEntityId.EntityId, out List<Tuple<long,EntityId>> releases))
                {
                    foreach (Tuple<long,EntityId> release in releases)
                    {
                        occupants.Remove(release.Item2);

                        commandSystem.SendResponse(new ResourceSchema.Resource.Release.Response
                        {
                            RequestId = release.Item1,
                            Payload = new ResourceSchema.ReleaseResponse(),
                        });
                    }
                }

                if (collecting.TryGetValue(spatialEntityId.EntityId, out List<Tuple<long, EntityId>> collects))
                {
                    foreach (Tuple<long, EntityId> collect in collects)
                    {
                        if (resourceHealth <= 0)
                        {
                            commandSystem.SendResponse(new ResourceSchema.Resource.Collect.Response
                            {
                                RequestId = collect.Item1,
                                Payload = new ResourceSchema.CollectResponse
                                {
                                    TimesUntilDepleted = 0,
                                    ResourceId = spatialEntityId.EntityId
                                }
                            });
                        }
                        else
                        {
                            //In future, maybe changes collect rate per unit? That's more balancing, not hard to implement.
                            resourceHealth -= 1;
                            commandSystem.SendResponse(new ResourceSchema.Resource.Collect.Response
                            {
                                RequestId = collect.Item1,
                                Payload = new ResourceSchema.CollectResponse
                                {

                                    DepleterId = resourceHealth <= 0 ? collect.Item2 : new EntityId(-1),
                                    ResourceId = spatialEntityId.EntityId,
                                    TimesUntilDepleted = resourceHealth
                                }
                            });
                        }
                    }
                }

                if (occupying.TryGetValue(spatialEntityId.EntityId, out List<Tuple<long, EntityId>> occupados))
                {
                    bool atMaxOccupants = occupants.Count == resourceMetadata.MaximumOccupancy;
                    foreach (Tuple<long, EntityId> occupod in occupados)
                    {
                        ResourceSchema.OccupyResponse occupyResponse = new ResourceSchema.OccupyResponse
                        {
                            Occupied = false,
                            ResourceId = spatialEntityId.EntityId
                        };
                        if (occupants.Contains(occupod.Item2))
                        {
                            occupyResponse.Occupied = true;
                        }                   
                        else if (!atMaxOccupants)
                        {
                            occupants.Add(occupod.Item2);
                            occupyResponse.Occupied = true;
                        }
                        // I was using failure message wrong. Operation didn't fail, the result of operation just ended up in diff result than wanted. Key difference.
                        atMaxOccupants = occupants.Count == resourceMetadata.MaximumOccupancy;
                        occupyResponse.FullyOccupied = atMaxOccupants;
                        //In future, maybe changes collect rate per unit? That's more balancing, not hard to implement.
                        commandSystem.SendResponse(new ResourceSchema.Resource.Occupy.Response
                        {
                            RequestId = occupod.Item1,
                            Payload = occupyResponse,

                        });
                    }
                }
                resource.Health = Unity.Mathematics.math.max(resourceHealth, 0);
                if (resource.Health == 0)
                {
                    occupants.Clear();
                }
                resource.Occupants = occupants;
            });
        }
    }
}