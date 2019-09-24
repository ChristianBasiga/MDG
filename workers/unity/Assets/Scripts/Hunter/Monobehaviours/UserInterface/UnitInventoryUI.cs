using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours;
using MDG.ScriptableObjects;
using InventorySchema = MdgSchema.Common.Inventory;
using MDG.Factories;
using UnityEngine.UI;

namespace MDG.Hunter.Monobehaviours
{
   
    public class UnitInventoryUI : MonoBehaviour
    {
        [Require] InventorySchema.InventoryReader InventoryReader;
        InventoryItemFactory itemFactory;

        public GameObject InventoryPanel;
        InventoryItem[] inventoryItems;
        ItemSlot[] itemCells;

        private void Start()
        {
            itemCells = new ItemSlot[InventoryPanel.transform.childCount];
            for (int i = 0; i < InventoryPanel.transform.childCount; ++i)
            {
                itemCells[i] = InventoryPanel.transform.GetChild(i).GetComponent<ItemSlot>();
            }
            itemFactory = new InventoryItemFactory();
            itemFactory.Initialize();
            // Replace this with zenject, this is different ver concrete inventory UI
            // as max inventroy space is different.
            InventoryReader.OnInventoryUpdate += UpdateUnitInventoryRender;
        }

        private void UpdateUnitInventoryRender(Dictionary<int, InventorySchema.Item> inventory)
        {
            Debug.Log("updating every frame??");
            for (int i = 0; i < itemCells.Length; ++i)
            {
                Debug.LogError("here" + itemCells[i].name);
                // Update slot.
                if (inventory.TryGetValue(i, out var item))
                {
                    Debug.Log("here???");
                    InventoryItem inventoryItem = itemFactory.GetInventoryItem(item.Id);
                    itemCells[i].UpdateSlot(inventoryItem);
                }
                // Make slot empty.
                else
                {
                    Debug.LogError("or here");
                    //If not in dictionary, then make it a blank slot. I might actually need item slot class then.
                    itemCells[i].ClearSlot();
                }
            }
        }
    }
}