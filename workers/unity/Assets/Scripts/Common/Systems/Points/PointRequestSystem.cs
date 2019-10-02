using Unity.Entities;
using System.Collections.Generic;
using PointSchema = MdgSchema.Common.Point;
using Improbable.Gdk.Core;

namespace MDG.Common.Systems
{
    public class PointRequestSystem : ComponentSystem
    {
        // I should reserve these.
        EntityId pointWorkerId = new EntityId(90);

        private Dictionary<long, PointSchema.PointRequest> requestIdToPayload;
        private Queue<PointSchema.PointRequest> pointRequests;
        CommandSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            pointRequests = new Queue<PointSchema.PointRequest>();
            requestIdToPayload = new Dictionary<long, PointSchema.PointRequest>();
            commandSystem = World.GetExistingSystem<CommandSystem>();

        }
        
        // Maybe make it take in something else to construct point request here to avoid including it in every file.
        public void AddPointRequest(PointSchema.PointRequest pointRequest)
        {
            pointRequests.Enqueue(pointRequest);
        }

        protected override void OnUpdate()
        {
            while (pointRequests.Count > 0)
            {
                PointSchema.PointRequest payload = pointRequests.Dequeue();
                //  Down line, set timeout millies as needed.
                long requestId = commandSystem.SendCommand(new PointSchema.Point.UpdatePoints.Request
                {
                    TargetEntityId = pointWorkerId,
                    Payload = payload

                });
                requestIdToPayload[requestId] = payload;
            }
            
            if (requestIdToPayload.Count > 0)
            {
                var responses = commandSystem.GetResponses<PointSchema.Point.UpdatePoints.ReceivedResponse>();
                for (int i = 0; i < responses.Count; ++i)
                {
                    ref readonly var response = ref responses[i];
                    if (requestIdToPayload.TryGetValue(response.RequestId, out PointSchema.PointRequest pointRequest))
                    {
                        requestIdToPayload.Remove(response.RequestId);
                        switch (response.StatusCode)
                        {
                            case Improbable.Worker.CInterop.StatusCode.Success:
                                UnityEngine.Debug.Log($"Points updated to {response.ResponsePayload.GetValueOrDefault()}");
                                break;
                            case Improbable.Worker.CInterop.StatusCode.Timeout:
                                // Requeue.
                                pointRequests.Enqueue(pointRequest);
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