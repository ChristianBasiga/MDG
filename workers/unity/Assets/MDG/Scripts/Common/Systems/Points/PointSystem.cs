﻿using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using PointSchema = MdgSchema.Common.Point;

namespace MDG.Common.Systems.Point
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class PointSystem : ComponentSystem
    {
        Dictionary<EntityId, int> idToPoints;
        EntityQuery pointGroup;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        EntityId pointWorkerId = new EntityId(4);

        float timeSinceIdleGain = 0;
        float gainRateInterval = 1.0f;
      
        protected override void OnCreate()
        {
            base.OnCreate();
            idToPoints = new Dictionary<EntityId, int>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            pointGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<PointSchema.PointMetadata.Component>(),
                ComponentType.ReadWrite<PointSchema.Point.Component>(),
                ComponentType.ReadOnly<PointSchema.Point.ComponentAuthority>(),
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>()
                );
            pointGroup.SetFilter(PointSchema.Point.ComponentAuthority.Authoritative);
        }


        // POint system sits on server. So will likely have requets.
        protected override void OnUpdate()
        {
            // Handles point change requests that aren't handled by jobs.
            var pointRequests = commandSystem.GetRequests<PointSchema.Point.UpdatePoints.ReceivedRequest>();
            for (int i = 0; i < pointRequests.Count; ++i)
            {
                ref readonly var request = ref pointRequests[i];
                var payload = request.Payload;
                // I mean, if I run initial job, this SHOULD always be true.
                if (idToPoints.TryGetValue(payload.EntityUpdating, out int currentPoints))
                {
                    int updatedPoints = currentPoints + payload.PointUpdate;
                    idToPoints[payload.EntityUpdating] = updatedPoints;
                }
                else
                {
                    idToPoints.Add(payload.EntityUpdating, request.Payload.PointUpdate);
                }
                commandSystem.SendResponse(new PointSchema.Point.UpdatePoints.Response
                {
                    RequestId = request.RequestId,
                    Payload = new PointSchema.PointResponse
                    {
                        TotalPoints = idToPoints[payload.EntityUpdating]
                    }
                }); 
            }


            // May refactor this later.
            if (timeSinceIdleGain <= 0)
            {
                Entities.With(pointGroup).ForEach((ref SpatialEntityId spatialEntityId, ref PointSchema.PointMetadata.Component pointMetaData, ref PointSchema.Point.Component point) =>
                {
                    int totalGain = point.Value + pointMetaData.IdleGainRate;
                    if (idToPoints.TryGetValue(spatialEntityId.EntityId, out int points))
                    {
                        totalGain += points;
                        idToPoints.Remove(spatialEntityId.EntityId);
                    }
                    totalGain = math.max(totalGain, 0);
                    point.Value = totalGain;
                });
                timeSinceIdleGain = gainRateInterval;
            }
            else
            {
                timeSinceIdleGain -= UnityEngine.Time.deltaTime;
            }
        }
    }
}