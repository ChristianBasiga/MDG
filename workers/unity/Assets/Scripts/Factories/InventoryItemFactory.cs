using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Factory
{
    //For now here, later on should move to own file maybe renderable namespace.
    //Inventory Item holds information for rendering.
    // mesh will be populated via mesh factory.
    public class RenderableItem
    {
        // 
    }

    public class InventoryItemFactory
    {

        private static Dictionary<int, InventoryItem> itemsCanRender;

        public static InventoryItem GetInventoryItem(int inventoryId)
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
    }
}