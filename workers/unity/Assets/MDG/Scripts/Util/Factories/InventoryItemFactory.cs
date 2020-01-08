using MDG.ScriptableObjects.Items;
using System.Collections.Generic;
using UnityEngine;
namespace MDG.Factories
{
    // Monobehaviour just for tseting rn.
    public class InventoryItemFactory
    {
        public readonly static int ResourceItemId = 1;
        private Dictionary<int, InventoryItem> itemsCanRender;
        public InventoryItem GetInventoryItem(int inventoryId)
        {
            if (itemsCanRender.TryGetValue(inventoryId, out InventoryItem inventoryItem))
            {
                return inventoryItem;
            }
            else
            {
                throw new System.Exception($"Item with id {inventoryId} does not exist");
            }
        }

        public void Initialize()
        {
            itemsCanRender = new Dictionary<int, InventoryItem>();
            InventoryItem[] items = Resources.LoadAll<InventoryItem>("ScriptableObjects/InventoryItems/");

            foreach (InventoryItem inventoryItem in items)
            {
               // temsCanRender.Add(inventoryItem.ItemId, inventoryItem);
            }
        }
    }
}