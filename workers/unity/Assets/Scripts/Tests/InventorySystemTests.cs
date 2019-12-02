using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Common.Components;
using MDG.Common.Systems.Inventory;
using NUnit.Framework;
using InventorySchema = MdgSchema.Common.Inventory;
// In future, due to how fucked unity ecs cross assembly testing is
// and the extra stuff it tests with their packages that I can't ignore.
// it would be better for me to create my own.
using Unity.Entities.Tests;
using MDG.Factories;
using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Items;
namespace MDG_Testing
{

    [TestFixture]
    [Category("Inventory System Tests")]
    public class InventorySystemTests : ECSTestsFixture
    {

        #region Client Tests
        [Test]
        public void PendingAddInventoryRemoveAfterRequest()
        {
            PendingInventoryAddition pendingInventoryAddition = new PendingInventoryAddition
            {
                ItemId = 1,
                Count = 1
            };
            SpatialEntityId spatialEntityId = new SpatialEntityId { EntityId = new EntityId(1) };
            Entity entity = m_Manager.CreateEntity(
                typeof(SpatialEntityId),
                typeof(PendingInventoryAddition),
                typeof(InventorySchema.Inventory.Component)
            );
            m_Manager.SetComponentData(entity, spatialEntityId);
            m_Manager.SetComponentData(entity, pendingInventoryAddition);
            World.CreateSystem<InventoryRequestSystem>().Update();
            Assert.That(!m_Manager.HasComponent<PendingInventoryAddition>(entity), "Pending Inventory Addition Not" +
                "Removed after sending request");
        }

        [Test]
        public void PendingRemoveInventoryRemoveAfterRequest()
        {
            PendingInventoryRemoval pendingInventoryAddition = new PendingInventoryRemoval
            {
                InventoryIndex = 1
            };
            SpatialEntityId spatialEntityId = new SpatialEntityId { EntityId = new EntityId(1) };
            Entity entity = m_Manager.CreateEntity(
                typeof(SpatialEntityId),
                typeof(PendingInventoryRemoval),
                typeof(InventorySchema.Inventory.Component)
            );
            m_Manager.SetComponentData(entity, spatialEntityId);
            m_Manager.SetComponentData(entity, pendingInventoryAddition);
            World.CreateSystem<InventoryRequestSystem>().Update();
            Assert.That(!m_Manager.HasComponent<PendingInventoryRemoval>(entity), "Pending Inventory Removal Not" +
                "Removed after sending request");
        }

        [Test]
        public void CorrectItemReceivedFromFactory()
        {
            InventoryItemFactory inventoryItemFactory = new InventoryItemFactory();
            inventoryItemFactory.Initialize();
            InventoryItem adding = new InventoryItem
            {
                PrefabPath = "Resource"
            };
           // InventoryItem added = inventoryItemFactory.GetInventoryItem(adding);
       //     Assert.AreEqual(adding, added);
        }
        #endregion
        #region Server Tests
        [Test]
        public void ItemRemovedFromInventory()
        {

            /*
            SpatialEntityId spatialEntityId = new SpatialEntityId { EntityId = new EntityId(1) };
            Entity entity = m_Manager.CreateEntity(
                typeof(SpatialEntityId),
                typeof(InventorySchema.Inventory.Component)
            );
            m_Manager.SetComponentData(entity, spatialEntityId);
            m_Manager.AddSharedComponentData(entity, InventorySchema.Inventory.ComponentAuthority.Authoritative);

            // Main issue is worker does not exist.
            // To automate this testing, I would need to make mocks of these.
            // no time now, or rather I'll test this in isolation manually rather than unit testing.

            
            World.CreateSystem<WorkerSystem>();
            CommandSystem commandSystem = World.CreateSystem<CommandSystem>();
            commandSystem.Enabled = true;
            commandSystem.Update();
            // Real question. How do I mock fucking requests yo. I suppose not the request I want to mock but the act of removal.
            World.CreateSystem<InventoryRequestHandlerSystem>().Update();
            commandSystem.SendCommand(new InventorySchema.Inventory.RemoveItemFromInventory.Request
            {
                Payload = new InventorySchema.InventoryRemoveItemRequest
                {
                    InventoryOwner = spatialEntityId.EntityId,
                    ItemPosition = 0
                }
            });*/
        }
        #endregion
    }
}