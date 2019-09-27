using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using MDG.Common.Components;
using InventorySchema = MdgSchema.Common.Inventory;

namespace MDG.Common.Systems.Inventory
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InventoryRequestSystem : ComponentSystem
    {
        public enum RequestType
        {
            Add,
            Remove
        }
        public struct RequestHeader
        {
            public EntityId InventoryOwner;
            // If addition, it is item id, if removal it is inventory Index.
            public int Key; 
            public RequestType RequestType;
            // Extra info stored in individual bits here. 
            public int? Count;
        }

        public struct RequestRetry
        {
            public RequestHeader RequestHeader;
            public int TimesRetried;
        }

        // Requests that haven't received responses yet.
        Dictionary<long, RequestRetry> pendingRequests;
        //Requests that failed due to time out queued to send request again.
        Queue<RequestRetry> requestsToRetry;
        const int maximumRequestRetries = 5;

        CommandSystem commandSystem;
        EntityQuery inventoryAddGroup;
        EntityQuery inventoryRemoveGroup;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            pendingRequests = new Dictionary<long, RequestRetry>();
            requestsToRetry = new Queue<RequestRetry>();
            inventoryAddGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<PendingInventoryAddition>(),
                ComponentType.ReadOnly<InventorySchema.Inventory.Component>());
            inventoryRemoveGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<PendingInventoryRemoval>(),
                ComponentType.ReadOnly<InventorySchema.Inventory.Component>());
        }
        protected override void OnUpdate()
        {
            try
            {
                SendRequests();
                ProcessResponses();
            }
            catch (System.Exception error)
            {
                // Replace use of this with logger.
                UnityEngine.Debug.Log(error);
            }
        }

        private void SendRequests()
        {
            // COULD be jobified, but optimizations not huge like this can be done later.
            // Thse loops could be done in parallel as well as iterations themselves.
            // hmm to an extent. Cause command system has no job support.

            // Send Add Requests.
            Entities.With(inventoryAddGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId, ref PendingInventoryAddition pendingInventoryAddition) =>
            {
                PostUpdateCommands.RemoveComponent<PendingInventoryAddition>(entity);

                RequestHeader requestHeader = new RequestHeader
                {
                    InventoryOwner = spatialEntityId.EntityId,
                    Key = pendingInventoryAddition.ItemId,
                    RequestType = RequestType.Add,
                    Count = pendingInventoryAddition.Count
                };
                long requestId = commandSystem.SendCommand<InventorySchema.Inventory.AddItemToInventory.Request>(new InventorySchema.Inventory.AddItemToInventory.Request
                {
                    TargetEntityId = new EntityId(4),
                    Payload = new InventorySchema.InventoryAddItemRequest
                    {
                        Count = requestHeader.Count.Value,
                        InventoryOwner = requestHeader.InventoryOwner,
                        ToAdd = new InventorySchema.Item
                        {
                            Id = requestHeader.Key
                        }
                    }
                });
                pendingRequests.Add(requestId, new RequestRetry
                {
                    RequestHeader = requestHeader,
                    TimesRetried = 0
                });
                
            });

            // Send remove requests.
            Entities.With(inventoryRemoveGroup).ForEach((Entity entity, ref SpatialEntityId spatialEntityId, ref PendingInventoryRemoval pendingInventoryRemoval) =>
            {
                PostUpdateCommands.RemoveComponent<PendingInventoryRemoval>(entity);

                // Should it be inventory index or or item id?
                // When does removal happen? Transferring to carrier. Carrer to player. Death.
                // all of which isn't specific in what is being sent. Maybe Invader has ability to choose
                // which to deposit. But removals will still be indices, cause extra item information
                // will only be in UI. This is fine.
                RequestHeader requestHeader = new RequestHeader
                {
                    InventoryOwner = spatialEntityId.EntityId,
                    Key = pendingInventoryRemoval.InventoryIndex,
                    RequestType = RequestType.Remove
                };
                
                long requestId = commandSystem.SendCommand<InventorySchema.Inventory.RemoveItemFromInventory.Request>(new InventorySchema.Inventory.RemoveItemFromInventory.Request
                {
                    TargetEntityId = new EntityId(4),
                    Payload = new InventorySchema.InventoryRemoveItemRequest
                    {
                        InventoryOwner = requestHeader.InventoryOwner,
                        ItemPosition = requestHeader.Key
                    }
                });
                pendingRequests.Add(requestId, new RequestRetry
                {
                    RequestHeader = requestHeader,
                    TimesRetried = 0
                });
            });

            // Dequeue Requests to retry.
            // I have feeling alot of this similiar code will be in most of my req response
            // systems. Should think about system inheritance soon For things like request header
            // and retry.
            while (requestsToRetry.Count > 0)
            {
                RequestRetry requestRetry = requestsToRetry.Dequeue();
                requestRetry.TimesRetried += 1;
                long? requestId = null;
                switch (requestRetry.RequestHeader.RequestType)
                {
                    case RequestType.Add:
                        requestId = commandSystem.SendCommand<InventorySchema.Inventory.AddItemToInventory.Request>(new InventorySchema.Inventory.AddItemToInventory.Request
                        {
                            Payload = new InventorySchema.InventoryAddItemRequest
                            {
                                Count = requestRetry.RequestHeader.Count.Value,
                                InventoryOwner = requestRetry.RequestHeader.InventoryOwner,
                                ToAdd = new InventorySchema.Item
                                {
                                    Id = requestRetry.RequestHeader.Key
                                }
                            }
                        });
                        break;
                    case RequestType.Remove:
                        requestId = commandSystem.SendCommand<InventorySchema.Inventory.RemoveItemFromInventory.Request>(new InventorySchema.Inventory.RemoveItemFromInventory.Request
                        {
                            Payload = new InventorySchema.InventoryRemoveItemRequest
                            {
                                InventoryOwner = requestRetry.RequestHeader.InventoryOwner,
                                ItemPosition = requestRetry.RequestHeader.Key
                            }
                        });
                        break;
                }
                // Readd it to pending requests.
                pendingRequests.Add(requestId.Value, requestRetry);
            }
        }

        private void ProcessResponses()
        {
            var responses = commandSystem.GetResponses<InventorySchema.Inventory.AddItemToInventory.ReceivedResponse>();

            for (int i = 0; i < responses.Count; ++i)
            {
                ref readonly var response = ref responses[i];
                if (pendingRequests.TryGetValue(response.RequestId, out RequestRetry requestRetry))
                {
                    pendingRequests.Remove(response.RequestId);

                    switch (response.StatusCode)
                    {
                        case Improbable.Worker.CInterop.StatusCode.Success:
                            break;
                        case Improbable.Worker.CInterop.StatusCode.Timeout:
                            //try again.
                            if (requestRetry.TimesRetried >= maximumRequestRetries)
                            {
                                throw new System.Exception("Reached Maximum Timeout Attempts: Failed to resolve inventory request");
                            }
                            requestsToRetry.Enqueue(requestRetry);
                            break;
                        default:
                            throw new System.Exception($"Failed to resolve request: {response.Message}");
                    }
                }
            }
        }
    }
}