using Improbable.Gdk.Core;
using ScriptableItems = MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.ScriptableObjects;
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