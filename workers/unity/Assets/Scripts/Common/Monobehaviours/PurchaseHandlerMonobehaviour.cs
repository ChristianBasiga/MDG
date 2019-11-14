using Improbable.Gdk.Subscriptions;
using MDG.ScriptableObjects.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Common.MonoBehaviours.Shopping
{

    public interface PurchaseHandler
    {
        bool HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject);
        // To say that purchase handler is for that linked entity component
        void Handshake(LinkedEntityComponent linkedEntityComponent);
    }
    public abstract class PurchaseHandlerMonobehaviour : MonoBehaviour
    {
        public abstract void HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject);
        public abstract void AddHandler(PurchaseHandler purchaseHandler);
    }
}
