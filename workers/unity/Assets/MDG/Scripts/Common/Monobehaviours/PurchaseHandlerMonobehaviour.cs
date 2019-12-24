using Improbable.Gdk.Subscriptions;
using MDG.ScriptableObjects.Items;
using UnityEngine;

namespace MDG.Common.MonoBehaviours.Shopping
{

    public interface IPurchaseHandler
    {
        bool HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject);
        // To say that purchase handler is for that linked entity component
        void Handshake(LinkedEntityComponent linkedEntityComponent);
    }
    public abstract class PurchaseHandlerMonobehaviour : MonoBehaviour
    {
        public abstract void HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject);
        public abstract void AddHandler(IPurchaseHandler purchaseHandler);
    }
}
