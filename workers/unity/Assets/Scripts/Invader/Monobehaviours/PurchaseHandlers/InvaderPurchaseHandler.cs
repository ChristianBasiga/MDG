using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.ScriptableObjects.Items;
using Improbable.Gdk.Subscriptions;

namespace MDG.Invader.Monobehaviours
{
    // No reason to extend further to chain of command.
    // Main reason not doing is handling adding references to these
    public class InvaderPurchaseHandler : PurchaseHandlerMonobehaviour
    {
        // Will be referencing Other handlers for managing spawning units, etc.
        List<IPurchaseHandler> purchaseHandlers;
       
        // Start is called before the first frame update
        void Start()
        {
            purchaseHandlers = new List<IPurchaseHandler>();
        }

        public override void HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject)
        {
            // Let all purchase handlers... handle purchase, don't break cause may want to do more stuff for unit purchase.
            for (int i = 0; i < purchaseHandlers.Count; ++i)
            {
                purchaseHandlers[i].HandlePurchase(shopItem, shopObject);
            }
            throw new System.Exception("No purchase handlers available to handle this purchase");
        }

        public override void AddHandler(IPurchaseHandler purchaseHandler)
        {
            purchaseHandler.Handshake(GetComponent<LinkedEntityComponent>());
            purchaseHandlers.Add(purchaseHandler);
        }
    }
}