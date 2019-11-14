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
        List<PurchaseHandler> purchaseHandlers;
       
        // Start is called before the first frame update
        void Start()
        {
            purchaseHandlers = new List<PurchaseHandler>();
        }

        public override void HandlePurchase(ShopItem shopItem, ShopBehaviour shopObject)
        {
            // Let all purchase handlers... handle purchase, don't break cause may want to do more stuff for unit purchase.
            foreach(PurchaseHandler purchaseHandler in purchaseHandlers)
            {
                if (purchaseHandler.HandlePurchase(shopItem, shopObject))
                {
                }
            }
            throw new System.Exception("No purchase handlers available to handle this purchase");
        }

        public override void AddHandler(PurchaseHandler purchaseHandler)
        {
            purchaseHandler.Handshake(GetComponent<LinkedEntityComponent>());
            purchaseHandlers.Add(purchaseHandler);
        }
    }
}