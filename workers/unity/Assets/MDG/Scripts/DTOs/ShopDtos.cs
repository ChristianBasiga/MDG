using MDG.ScriptableObjects;
using System;
using UnitSchema = MdgSchema.Units;

namespace MDG.DTO
{
    [Serializable]
    public class PurchasePayload
    {
        public ShopItemDto shopItem;
        public long purchaserId;
    }

    [Serializable]
    public class ShopItemDto
    {
        public Constants.ShopItemType shopItemType;
    }

    [Serializable]
    public class ShopUnitDto: ShopItemDto
    {
        public UnitSchema.UnitTypes unitType;
        public float constructionTime;
    }
}