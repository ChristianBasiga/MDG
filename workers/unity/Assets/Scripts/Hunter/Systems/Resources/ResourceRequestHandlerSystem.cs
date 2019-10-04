using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using ResourceSchema = MdgSchema.Game.Resource;
using System;
using MdgSchema.Common.Spawn;

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

        private void ProcessRequests()
        {
            if (resourceGroup.CalculateEntityCount() == 0) return;
            ResourceUpdatePayload resourceUpdatePayload = new ResourceUpdatePayload
            {
               occupying = new Dictionary<EntityId, List<Tuple<long, EntityId>>>(),
               collecting = new Dictionary<EntityId, List<Tuple<long, EntityId>>>(),
               releasing = new Dictionary<EntityId, List<Tuple<long, EntityId>>>()
            };
               
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
            }

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
            }


            var collectRequests = commandSystem.GetRequests<ResourceSchema.Resource.Collect.ReceivedRequest>();

            for (int i = 0; i < collectRequests.Count; ++i)
            {
                ref readonly var request = ref collectRequests[i];
                var collecting = resourceUpdatePayload.collecting;
                if (!collecting.TryGetValue(request.EntityId, out _))
                {
                    collecting[request.EntityId] = new List<Tuple<long, EntityId>>();
                }
                collecting[request.EntityId].Add(new Tuple<long, EntityId>(request.RequestId, request.Payload.CollectorId));
            }
            Entities.With(resourceGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId, ref ResourceSchema.ResourceMetadata.Component resourceMetadata, ref ResourceSchema.Resource.Component resource) =>
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