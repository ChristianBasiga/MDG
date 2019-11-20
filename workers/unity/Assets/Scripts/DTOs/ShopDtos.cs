using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.DTO
{
    [SerializeField]
    public class PurchasePayload
    {
        public ShopItem shopItem;
        public long purchaserId;
    }
}