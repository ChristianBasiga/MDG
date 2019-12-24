using MDG.ScriptableObjects;
using System;
using UnitSchema = MdgSchema.Units;

namespace MDG.DTO
{
    [Serializable]
    public class PurchasePayload
    {
        public ShopItemDto ShopItem;
        public long PurchaserId;
    }

    [Serializable]
    public class ShopItemDto
    {
        public Constants.ShopItemType ShopItemType;
    }

    [Serializable]
    public class ShopUnitDto: ShopItemDto
    {
        public UnitSchema.UnitTypes UnitType;
        public float ConstructionTime;
    }
}