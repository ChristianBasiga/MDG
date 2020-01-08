using Improbable.Gdk.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using PointSchema = MdgSchema.Common.Point;

namespace MDG.Common.Systems.Point
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]   
    public class PointRequestSystem : ComponentSystem
    {
        public class PointRequestPayload
        {
            public int pointUpdate;
            public List<Action<PointSchema.Point.UpdatePoints.ReceivedResponse>> callbacks;
        }

        private Dictionary<long, PointRequestPayload> requestIdToPayload;
        private Dictionary<EntityId, PointRequestPayload> pointRequests;
        private CommandSystem commandSystem;


        protected override void OnCreate()
        {
            base.OnCreate();
            pointRequests = new Dictionary<EntityId, PointRequestPayload>();
            requestIdToPayload = new Dictionary<long, PointRequestPayload>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }
        
        // Maybe make it take in something else to construct point request here to avoid including it in every file.
        // Add callback here. Callback will undo point update if fails, etc.
        public void AddPointRequest(PointSchema.PointRequest pointRequest, Action<PointSchema.Point.UpdatePoints.ReceivedResponse> callback = null)
        {
            if (pointRequests.TryGetValue(pointRequest.EntityUpdating, out PointRequestPayload pointRequestPayload))
            {
                pointRequestPayload.pointUpdate += pointRequest.PointUpdate;
                if (callback != null)
                {
                    pointRequestPayload.callbacks.Add(callback);
                }
                pointRequests[pointRequest.EntityUpdating] = pointRequestPayload;
            }
            else
            {

                List<Action<PointSchema.Point.UpdatePoints.ReceivedResponse>> callbacks = new List<Action<PointSchema.Point.UpdatePoints.ReceivedResponse>>();
                if (callback != null)
                {
                    callbacks.Add(callback);
                }
                pointRequests.Add(pointRequest.EntityUpdating, new PointRequestPayload
                {
                    callbacks = callbacks,
                    pointUpdate = pointRequest.PointUpdate
                });
            }
        }
        protected override void OnUpdate()
        {
            Entities.ForEach((ref SpatialEntityId spatialEntityId, ref PointSchema.Point.Component point) =>
            {
                EntityId entityId = spatialEntityId.EntityId;
                if (pointRequests.TryGetValue(entityId, out PointRequestPayload pointRequestPayload))
                {
                    PointSchema.PointRequest pointRequest = new PointSchema.PointRequest
                    {
                        EntityUpdating = entityId,
                        PointUpdate = pointRequestPayload.pointUpdate
                    };
                    long requestId = commandSystem.SendCommand(new PointSchema.Point.UpdatePoints.Request
                    {
                        TargetEntityId = entityId,
                        Payload = pointRequest
                    });
                    requestIdToPayload[requestId] = pointRequestPayload;
                }
            });
            pointRequests.Clear();

            // Process response only ever other frame.
            ProcessPointResponses();
        }

        private void ProcessPointResponses()
        {
             if (requestIdToPayload.Count > 0)
            {
                var responses = commandSystem.GetResponses<PointSchema.Point.UpdatePoints.ReceivedResponse>();
                for (int i = 0; i<responses.Count; ++i)
                {
                    ref readonly var response = ref responses[i];
                    if (requestIdToPayload.TryGetValue(response.RequestId, out PointRequestPayload pointRequest))
                    {
                        requestIdToPayload.Remove(response.RequestId);
                        switch (response.StatusCode)
                        {
                            case Improbable.Worker.CInterop.StatusCode.Success:
                                for (int j = 0; j < pointRequest.callbacks.Count; ++j)
                                {
                                    pointRequest.callbacks[j]?.Invoke(response);
                                }
                                break;
                            case Improbable.Worker.CInterop.StatusCode.Timeout:
                                // Requeue.
                                UnityEngine.Debug.Log("Timed out");
                                pointRequests.Add(response.EntityId, pointRequest);
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