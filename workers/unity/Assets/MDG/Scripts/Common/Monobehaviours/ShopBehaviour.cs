using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using MDG.Common.Systems.Point;
using MDG.ScriptableObjects.Items;
using MdgSchema.Common.Point;
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

        public delegate void ShopActionEventHandler(ShopItem shopItem, LinkedEntityComponent purchaser);
        public event ShopActionEventHandler OnPurchaseItem;

        public void TryPurchase(ShopItem shopItem, LinkedEntityComponent purchaser)
        {
            if (purchaser.Worker.TryGetEntity(purchaser.EntityId, out Unity.Entities.Entity purchaserEntity))
            {
                Point.Component pointComponent = purchaser.World.EntityManager.GetComponentData<Point.Component>(purchaserEntity);

                if (pointComponent.Value <= shopItem.Cost)
                {
                    ShowCantPurchaseUI("Not enough points");
                }
                else
                {
                    // Otherwise send point request to decrease points
                    // and invoke purchase handler.
                    PointRequestSystem pointRequestSystem = purchaser.World.GetExistingSystem<PointRequestSystem>();
                    pointRequestSystem.AddPointRequest(new PointRequest
                    {
                        EntityUpdating = purchaser.EntityId,
                        PointUpdate = -shopItem.Cost
                    }, OnPointRequestReturned);
                    // Will handle purchase right away or on call back??
                    //purchaser.GetComponent<PurchaseHandlerMonobehaviour>().HandlePurchase(shopItem, this);
                    OnPurchaseItem?.Invoke(shopItem, purchaser);
                }
            }
        }

        void OnPointRequestReturned(Point.UpdatePoints.ReceivedResponse receivedResponse)
        {
            if (receivedResponse.StatusCode != StatusCode.Success)
            {
                ShowCantPurchaseUI(receivedResponse.Message);
            }
        }

        private void ShowCantPurchaseUI(string message)
        {
            // Do this later.
            Debug.Log($"Failed to purchase: {message}");
        }
    }
}