using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{
    // Remove at end of tick, prob fine for now but will do manual removal later.
    // incase many pendings that may execute over multiple frames.
   
    // Added to every entity that needs addition to Inventory.
    [RemoveAtEndOfTick]
    public struct PendingInventoryAddition : IComponentData
    {
        public int ItemId;
        public int Count;
    }

    // Added to every entity that needs Inventory Item Removal.
    [RemoveAtEndOfTick]
    public struct PendingInventoryRemoval : IComponentData
    {
        public int InventoryIndex;
    }

    // Added to every entity that can be an item added to inventory.
    // The item itself is not an entity but simply has a UI representation and an integer in Inventroy components
    // of existing entities.
    public struct ItemMetaData : IComponentData
    {
        public int ItemId;
    }
}