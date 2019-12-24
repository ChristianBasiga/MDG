using MdgSchema.Common.Inventory;
using System;
using System.Collections.Generic;

// Move these comments to read me of dtos.
// Move all DTOs to be scriptable objects instead.
// make testing UI alot easier as well as making these specific instances much more scalalbe.
namespace MDG.DTO
{

    [Serializable]
    public class InventoryConfig
    {
        public long ownerId;
        public int pointCost;
        public int inventorySize;
        // This would be specific jobs to run in structure.
        public Dictionary<int, Item> itemToCost;
    }

    [Serializable]
    public class ItemConfig
    {
        public Cost cost;
    }

    [Serializable]
    public class InvaderItemConfig : ItemConfig
    {
        public int minWorkers;
    }
}
