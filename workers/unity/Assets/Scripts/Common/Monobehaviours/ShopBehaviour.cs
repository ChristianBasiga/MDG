using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using MDG.Common.Systems.Point;
using MDG.ScriptableObjects.Items;
using MdgSchema.Common.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Common.MonoBehaviours.Shopping
{

    /// <summary>
    /// Generic PointSpend behaviour that all buttons for placing traps, spawning minions, structures, etc.
    /// It recieves entityId of spending entity, and the payload of purchase. Exist on it's own or exist as
    /// behaviour on player? Latter is problably better tbh. That way on click is simply passing payload since we know who is spending it.
    /// </summary>
    public class ShopBehaviour : MonoBehaviour
    {
        ShopItem pendingPurchase;
        GameObject shopObject;
        LinkedEntityComponent purchaser;

        public delegate void ShopActionEventHandler(ShopItem shopItem, GameObject purchaser);
        public event ShopActionEventHandler OnPurchaseItem;
        // Start is called before the first frame update
        void Start()
        {
        }

        // So two things here, the shop item and the belonging shop gameobject.
        public void OnShopItem(ShopItem shopItem, GameObject purchaser)
        {
            // Trigger callbacks for all actions done subscribed by other components on same object.
            OnPurchaseItem?.Invoke(shopItem, purchaser);

            // Update Point and invoice PurchaseHandlers.
            LinkedEntityComponent linkedEntityComponent = purchaser.GetComponent<LinkedEntityComponent>();

            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Unity.Entities.Entity purchaserEntity))
            {
                Point.Component pointComponent = linkedEntityComponent.World.EntityManager.GetComponentData<Point.Component>(purchaserEntity);

                if (pointComponent.Value <= shopItem.Cost)
                {
                    ShowCantPurchaseUI();
                }
                else
                {
                    // Otherwise send point request to decrease points
                    // and invoke purchase handler.
                    PointRequestSystem pointRequestSystem = linkedEntityComponent.World.GetExistingSystem<PointRequestSystem>();

                    pointRequestSystem.AddPointRequest(new PointRequest
                    {
                        EntityUpdating = linkedEntityComponent.EntityId,
                        PointUpdate = -shopItem.Cost
                    }, OnPointRequestReturned);
                    // Will handle purchase right away or on call back??
                    purchaser.GetComponent<PurchaseHandlerMonobehaviour>().HandlePurchase(shopItem, this);
                }
            }

        }

        void OnPointRequestReturned(Point.UpdatePoints.ReceivedResponse receivedResponse)
        {
            if (receivedResponse.StatusCode != StatusCode.Success)
            {
                Debug.LogError(receivedResponse.Message);
                ShowCantPurchaseUI();
            }
        }

        private void ShowCantPurchaseUI()
        {
            Debug.Log("Can't purchase");
        }
    }
}