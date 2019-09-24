using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using InventorySchema = MdgSchema.Common.Inventory;
using Improbable.Gdk.Core;

namespace MDG.Common.Systems.Inventory {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InventoryRequestHandlerSystem : ComponentSystem
    {
        NativeQueue<InventorySchema.InventoryServiceResponse> queuedResponses;
        NativeList<InventorySchema.InventoryAddItemRequest> addItemRequests;
        NativeList<InventorySchema.InventoryRemoveItemRequest> removeItemRequests;
        EntityQuery inventoryGroup;
        CommandSystem commandSystem;


        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            // For me to enforce this authoritative access, cannot jobify this due to lack of SpatialOS job system support.
            inventoryGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<InventorySchema.Inventory.Component>(),
                ComponentType.ReadOnly<InventorySchema.Inventory.ComponentAuthority>()
                );
            inventoryGroup.SetFilter(InventorySchema.Inventory.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            try
            {
                HandleRequests();
            }
            catch(System.Exception error)
            {
                //Replace this with logger.
                Debug.LogError(error);
            }
        }

        private void HandleRequests()
        {
            // Don't care for target worker entity for now.
            // I'll add that infrastructure as need more server workers later.
            // For now it is not an issue a single instance of each worker should be fine.

            int inventoryCount = inventoryGroup.CalculateEntityCount();
            // For when I jobify, I'll need this.
            //addItemRequests = new NativeList<InventorySchema.InventoryAddItemRequest>(inventoryCount, Allocator.TempJob);
            //removeItemRequests = new NativeList<InventorySchema.InventoryRemoveItemRequest>(inventoryCount, Allocator.TempJob);

            Dictionary<EntityId, List<InventorySchema.Inventory.AddItemToInventory.ReceivedRequest>> addItemRequests = new Dictionary<EntityId, List<InventorySchema.Inventory.AddItemToInventory.ReceivedRequest>>();
            Dictionary<EntityId, List<InventorySchema.Inventory.RemoveItemFromInventory.ReceivedRequest>> removeItemRequests = new Dictionary<EntityId, List<InventorySchema.Inventory.RemoveItemFromInventory.ReceivedRequest>>();


            var addRequests = commandSystem.GetRequests<InventorySchema.Inventory.AddItemToInventory.ReceivedRequest>(new EntityId(4));
            UnityEngine.Debug.LogError(addRequests.Count);
            for (int i = 0; i < addRequests.Count; ++i)
            {
                Debug.LogError("received request");
                ref readonly var request = ref addRequests[i];
                if (!addItemRequests.ContainsKey(request.Payload.InventoryOwner))
                {
                    addItemRequests[request.Payload.InventoryOwner] = new List<InventorySchema.Inventory.AddItemToInventory.ReceivedRequest>();
                }
                addItemRequests[request.Payload.InventoryOwner].Add(request);
            }

            var removeRequests = commandSystem.GetRequests<InventorySchema.Inventory.RemoveItemFromInventory.ReceivedRequest>();
            for (int i = 0; i < removeRequests.Count; ++i)
            {
                ref readonly var request = ref removeRequests[i];
                if (!removeItemRequests.ContainsKey(request.Payload.InventoryOwner))
                {
                    removeItemRequests[request.Payload.InventoryOwner] = new List<InventorySchema.Inventory.RemoveItemFromInventory.ReceivedRequest>();
                }
                removeItemRequests[request.Payload.InventoryOwner].Add(request);
            }

            if (addItemRequests.Count == 0 && removeItemRequests.Count == 0) return;
            Entities.With(inventoryGroup).ForEach((ref SpatialEntityId spatialEntityId, ref InventorySchema.Inventory.Component inventoryComponent) =>
            {
                Dictionary<int, InventorySchema.Item> inventory = inventoryComponent.Inventory;

                Queue<int> freeSlots = new Queue<int>();

                for(int i = 0; i < inventoryComponent.InventorySize; ++i)
                {
                    if (!inventory.ContainsKey(i))
                    {
                        freeSlots.Enqueue(i);
                    }
                }
                // Do removals first.
                // Then do additions, so that false full inventory won't happen.
                if (removeItemRequests.TryGetValue(spatialEntityId.EntityId, out var removeItemRequest))
                {
                    foreach (var req in removeItemRequest)
                    {
                        inventory.Remove(req.Payload.ItemPosition);

                        commandSystem.SendResponse(new InventorySchema.Inventory.RemoveItemFromInventory.Response
                        {
                            RequestId = req.RequestId,
                            Payload = new InventorySchema.InventoryServiceResponse
                            {
                                Success = true
                            }
                        });
                        freeSlots.Enqueue(req.Payload.ItemPosition);
                    }
                }
                // Then add the items to inventory filling up empty slots in order.
                if (addItemRequests.TryGetValue(spatialEntityId.EntityId, out var addItemRequest))
                {
                    foreach(var req in addItemRequest)
                    {
                        if (freeSlots.Count == 0)
                        {
                            commandSystem.SendResponse(new InventorySchema.Inventory.AddItemToInventory.Response
                            {
                                RequestId = req.RequestId,
                                Payload = new InventorySchema.InventoryServiceResponse
                                {
                                    Success = false,
                                    Error = $"Failed to add item {req.Payload.ToAdd} to inventory, no free slots.",
                                    ErrorType = InventorySchema.InventoryRequestErrorTypes.INVENTORY_FULL
                                }
                            });
                        }
                        else
                        {
                            int freeSlot = freeSlots.Dequeue();
                            inventory.Add(freeSlot, req.Payload.ToAdd);
                            commandSystem.SendResponse(new InventorySchema.Inventory.AddItemToInventory.Response
                            {
                                RequestId = req.RequestId,
                                Payload = new InventorySchema.InventoryServiceResponse
                                {
                                    Success = true
                                }
                            });
                        }
                    }
                }
                inventoryComponent.Inventory = inventory;
            });
        }
    }
}