using Improbable.Gdk.Core.Commands;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using TerritorySchema = MdgSchema.Game.Territory;
using StructureSchema = MdgSchema.Common.Structure;
using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace MDG.Game.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class TerritoryMonitorSystem : ComponentSystem
    {
        EntityQuery territoryQuery;
        CommandSystem commandSystem;
        NativeArray<SpatialEntityId> territoryIds;
        NativeArray<Entity> territoryEntities;
        protected override void OnCreate()
        {
            base.OnCreate();
            territoryQuery = GetEntityQuery(
                ComponentType.ReadOnly<TerritorySchema.Territory.Component>(),
                ComponentType.ReadOnly<TerritorySchema.TerritoryStatus.Component>(),
                ComponentType.ReadWrite<TerritorySchema.TerritoryStatus.ComponentAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );
            territoryIds = territoryQuery.ToComponentDataArray<SpatialEntityId>(Unity.Collections.Allocator.Persistent);
            territoryEntities = territoryQuery.ToEntityArray(Allocator.Persistent);
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            territoryIds.Dispose();
            territoryEntities.Dispose();
        }


        struct UpdateTerritoryStatusJob : IJobForEach<TerritorySchema.Territory.Component, TerritorySchema.TerritoryStatus.Component, SpatialEntityId>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, TerritorySchema.UpdateTerritoryStatusRequest> updates;

            public float deltaTime;

            public void Execute([ReadOnly] ref TerritorySchema.Territory.Component territoryComponent, ref TerritorySchema.TerritoryStatus.Component territoryStatusComponent, 
                [ReadOnly] ref SpatialEntityId spatialEntityId)
            {
                if (updates.TryGetValue(spatialEntityId.EntityId, out TerritorySchema.UpdateTerritoryStatusRequest req))
                {
                    if (req.Status != territoryStatusComponent.Status)
                    {
                        territoryStatusComponent.Status = req.Status;
                    }
                }

                switch (territoryStatusComponent.Status)
                {
                    case TerritorySchema.TerritoryStatusTypes.Claiming:
                        float updatedProgress = territoryStatusComponent.ClaimProgress + deltaTime;
                        updatedProgress = math.min(updatedProgress, territoryComponent.TimeToClaim);
                        territoryStatusComponent.ClaimProgress += deltaTime;
                        if (territoryStatusComponent.ClaimProgress == territoryComponent.TimeToClaim)
                        {
                            territoryStatusComponent.Status = TerritorySchema.TerritoryStatusTypes.Claimed;
                        }
                        break;
                    case TerritorySchema.TerritoryStatusTypes.Released:
                        territoryStatusComponent.ClaimProgress = 0;
                        break;
                    case TerritorySchema.TerritoryStatusTypes.Claimed:

                        break;
                }
            }
        }

        protected override void OnUpdate()
        {

            NativeHashMap<EntityId, TerritorySchema.UpdateTerritoryStatusRequest> territoryStatusRequests = 
                new NativeHashMap<EntityId, TerritorySchema.UpdateTerritoryStatusRequest>(territoryIds.Length, Allocator.TempJob);

            for (int i = 0; i < territoryIds.Length; ++i)
            {
                var requests = commandSystem.GetRequests<TerritorySchema.TerritoryStatus.UpdateClaim.ReceivedRequest>(territoryIds[i].EntityId);
                if (requests.Count > 0)
                {
                    ref readonly var request = ref requests[i];
                    territoryStatusRequests[territoryIds[i].EntityId] = request.Payload;

                    commandSystem.SendResponse(new TerritorySchema.TerritoryStatus.UpdateClaim.Response
                    {
                        RequestId = request.RequestId,
                        Payload = new TerritorySchema.UpdateTerritoryStatusResponse()
                    });
                }
            }
            territoryStatusRequests.Dispose();
        }
    }
}