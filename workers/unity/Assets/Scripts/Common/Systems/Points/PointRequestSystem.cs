using Unity.Entities;
using System.Collections.Generic;
using PointSchema = MdgSchema.Common.Point;
using Improbable.Gdk.Core;
using System;
using System.Linq;

namespace MDG.Common.Systems.Point
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]   
    public class PointRequestSystem : ComponentSystem
    {
        public struct PointRequestPayload
        {
            public PointSchema.PointRequest payload;
            public Action<PointSchema.PointResponse> callback;
        }
        private Dictionary<long, PointRequestPayload> requestIdToPayload;
        private List<PointRequestPayload> pointRequests;
        private CommandSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            pointRequests = new List<PointRequestPayload>();
            requestIdToPayload = new Dictionary<long, PointRequestPayload>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }
        
        // Maybe make it take in something else to construct point request here to avoid including it in every file.
        // Add callback here. Callback will undo point update if fails, etc.
        public void AddPointRequest(PointSchema.PointRequest pointRequest, Action<PointSchema.PointResponse> callback = null)
        {
            pointRequests.Add(new PointRequestPayload
            {
                payload = pointRequest,
                callback = callback
            });
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref SpatialEntityId spatialEntityId, ref PointSchema.Point.Component point) =>
            {
                EntityId entityId = spatialEntityId.EntityId;
                IEnumerable<PointRequestPayload> pointRequestPayloads = pointRequests.Where((PointRequestPayload p) =>
                {
                    UnityEngine.Debug.Log(p.payload.EntityUpdating.Equals(entityId));
                    return p.payload.EntityUpdating.Equals(entityId);
                });

                foreach(var requestPayload in pointRequestPayloads)
                {
                    long requestId = commandSystem.SendCommand(new PointSchema.Point.UpdatePoints.Request
                    {
                        TargetEntityId = entityId,
                        Payload = requestPayload.payload,
                        AllowShortCircuiting = true
                    });
                    requestIdToPayload[requestId] = requestPayload;
                }

            });
            pointRequests.Clear();
            
            if (requestIdToPayload.Count > 0)
            {
                var responses = commandSystem.GetResponses<PointSchema.Point.UpdatePoints.ReceivedResponse>();
                for (int i = 0; i < responses.Count; ++i)
                {
                    ref readonly var response = ref responses[i];
                    if (requestIdToPayload.TryGetValue(response.RequestId, out PointRequestPayload pointRequest))
                    {
                        requestIdToPayload.Remove(response.RequestId);
                        switch (response.StatusCode)
                        {
                            case Improbable.Worker.CInterop.StatusCode.Success:
                                UnityEngine.Debug.Log($"Points updated to {response.ResponsePayload.GetValueOrDefault().EntityUpdated} " +
                                    $"having {response.ResponsePayload.GetValueOrDefault().TotalPoints}");
                                pointRequest.callback.Invoke(response.ResponsePayload.GetValueOrDefault());
                                break;
                            case Improbable.Worker.CInterop.StatusCode.Timeout:
                                // Requeue.
                                UnityEngine.Debug.Log("Timed out");
                                pointRequests.Add(pointRequest);
                                break;
                            default:
                                // Throw error.
                                UnityEngine.Debug.LogError(response.Message);
                                break;
                        }
                    }
                }
            }
        }
    }
}