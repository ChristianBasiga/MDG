using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using UnityEngine.SceneManagement;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Unity.Entities;
using MDG.Common.Components;
using InventorySystems = MDG.Common.Systems.Inventory;
using InventoryComponents = MDG.Common.Components;
using InventorySchema = MdgSchema.Common.Inventory;
using MDG.Factories;
using MDG.ClientSide;
using MDG.ClientSide.UserInterface;
using MDG.ScriptableObjects.Items;

namespace PlaymodeTests
{
    public class InventorySystemTest
    {
        LinkedEntityComponent linkedEntityComponent;
        WorkerSystem workerSystem;
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest, Order(1)]
        public IEnumerator SceneValidation()
        {
            SceneManager.LoadScene("DevelopmentScene");
            GameObject uiManager = null;

            yield return new WaitUntil(() =>
            {
                uiManager = GameObject.Find("ClientWorker");
                return uiManager != null;

            });
            yield return new WaitForSeconds(2.0f);
            uiManager.GetComponent<UIManager>().SelectRole("Hunter");
            yield return new WaitUntil(() =>
            {
                return GameObject.Find("Hunter_Spawned") != null && GameObject.FindGameObjectWithTag("Unit") != null;
            });
            linkedEntityComponent = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>() ;

        }
        [UnityTest, Order(2)]
        public IEnumerator InventoryAddTest()
        {

            workerSystem = linkedEntityComponent.Worker;
            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;

                InventorySchema.Inventory.Component initialInventory = entityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);
                Assert.True(initialInventory.Inventory.Count == 0, "Not starting with empty inventory");
                InventorySchema.Item itemAdding = new InventorySchema.Item { Id = InventoryItemFactory.ResourceItemId };

                // Just replacing this with sending requests.
                entityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryAddition
                {
                    Count = 1,
                    ItemId = itemAdding.Id
                });
                yield return new WaitForEndOfFrame();

                // Request system should have removed it.
                Assert.IsTrue(!entityManager.HasComponent<InventoryComponents.PendingInventoryAddition>(entity), "Pending Inventory addition not removed");

                // 4 frames, for queueing inventory addtion, sending request, update component, server to client sync.
                for (int i = 0; i < 4; ++i)
                {
                    yield return new WaitForEndOfFrame();
                }
                InventorySchema.Inventory.Component updatedInventory = entityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);
                // Confirm that the correct item has been added to inventory.
                Assert.True(updatedInventory.Inventory.Count == 1, "Inventory not updated with new item");
                Assert.True(updatedInventory.Inventory.ContainsValue(itemAdding), "Inventory not updated with correct item");
                InventoryItemFactory inventoryItemFactory = new InventoryItemFactory();
                inventoryItemFactory.Initialize();
                InventoryItem renderableItem = inventoryItemFactory.GetInventoryItem(updatedInventory.Inventory[0].Id);
                Assert.True(renderableItem.Equals(inventoryItemFactory.GetInventoryItem(itemAdding.Id)), "Item didn't result in correct render");
                entityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryAddition
                {
                    Count = 1,
                    ItemId = itemAdding.Id
                });
                yield return new WaitForSeconds(2.0f);
                Assert.True(updatedInventory.Inventory.Count == 2, "Inventory not adding more than 1 item");
            }
        }

        [UnityTest, Order(3)]
        public IEnumerator InventoryRemoveTest()
        {

            WorkerSystem workerSystem = linkedEntityComponent.World.GetExistingSystem<WorkerSystem>();

            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;

                InventorySchema.Inventory.Component initialInventory = entityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);
                Assert.True(initialInventory.Inventory.Count == 2, "Not starting with item in inventory");
                int indexRemoving = 1;
                entityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryRemoval
                {
                    InventoryIndex = indexRemoving
                });
                yield return null;
                workerSystem.World.GetOrCreateSystem<InventorySystems.InventoryRequestSystem>();
                yield return null;
                // Request system should have removed it.
                Assert.IsTrue(!entityManager.HasComponent<InventoryComponents.PendingInventoryRemoval>(entity), "Pending Inventory addition not removed");
                yield return new WaitForSeconds(2.0f);
                InventorySchema.Inventory.Component updatedInventory = entityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);

                // Confirm that the correct item has been added to inventory.
                Assert.True(updatedInventory.Inventory.Count == 1, "Item not removed from inventory");
                Assert.True(!updatedInventory.Inventory.ContainsKey(indexRemoving), "Incorrect item removed");

                entityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryRemoval
                {
                    InventoryIndex = 0
                });
                yield return new WaitForSeconds(2.0f);
                Assert.True(updatedInventory.Inventory.Count == 0, "Both items not removed");
            }

        }
        [UnityTest, Order(4)]
        public IEnumerator InventoryFilledTest()
        {
            yield return null;
            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;
                InventorySchema.Inventory.Component initialInventory = entityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);
                // Fills up inventory.
                for (int i = 0; i < initialInventory.InventorySize + 1; ++i)
                {
                    // This is issue actually, fuck. This makes it so I CAN'T add to same inventory within same frame.
                    // Okay, this settles it. I have to turn this to same way I did spawn system and resource system.
                    // that is just the best way.
                    entityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryAddition
                    {
                        Count = 1,
                        ItemId = InventoryItemFactory.ResourceItemId
                    });
                    yield return null;
                }
                yield return new WaitForSeconds(2.0f);
                Assert.False(initialInventory.Inventory.Count < initialInventory.InventorySize, "Failed to fill inventory");
                Assert.False(initialInventory.Inventory.Count > initialInventory.InventorySize, "Added past the set capacity");
            }
        }
        [UnityTest, Order(5)]
        public IEnumerator FillingCorrectEmptySlotTest()
        {
            yield return null;
            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                InventorySchema.Inventory.Component inventory = workerSystem.EntityManager.GetComponentData<InventorySchema.Inventory.Component>(entity);
                int indexRemoving = (int)(inventory.InventorySize / 2);
                workerSystem.EntityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryRemoval
                {
                    InventoryIndex = (int)(inventory.InventorySize / 2)
                });
                yield return new WaitForSeconds(2.0f);
                Assert.False(inventory.Inventory.Count.Equals(inventory.InventorySize), "Failed to remove item");
                Assert.False(inventory.Inventory.ContainsKey(indexRemoving), "Removed the incorrect position)");
                workerSystem.EntityManager.AddComponentData(entity, new InventoryComponents.PendingInventoryAddition
                {
                    Count = 1,
                    ItemId = InventoryItemFactory.ResourceItemId
                });
                yield return new WaitForSeconds(2.0f);
                Assert.True(inventory.Inventory.ContainsKey(indexRemoving), "Inserted in incorrect position");
            }
        }
    }
}
