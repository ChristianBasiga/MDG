using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG;
using MdgSchema.Common.Point;
using MdgSchema.Units;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MDG.Invader.Monobehaviours
{
    public class InvaderHud : MonoBehaviour
    {
        [Require] PointReader pointReader;
        Text numberOfUnits;
        Text pointText;
        EntityQuery unitQuery;
        void Start()
        {
            pointReader.OnValueUpdate += UpdatePointText;
            // Setup for updating unit count.
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            unitQuery = linkedEntityComponent.Worker.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Unit.Component>()
                );
            UnityClientConnector unityClientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            unityClientConnector.clientGameObjectCreator.OnEntityAdded += UpdateUnitCount;
            unityClientConnector.clientGameObjectCreator.OnEntityDeleted += UpdateUnitCount;
        }

        private void UpdatePointText(int pointValue)
        {
            pointText.text = pointValue.ToString();
        }

        // Just calculate entity count each time, should update without recreating query.
        // Could only calculate count if the updated is of Unit type. But that's small change can make later.
        private void UpdateUnitCount(EntityId entityId)
        {
            int newUnitCount = unitQuery.CalculateEntityCount();
            numberOfUnits.text = newUnitCount.ToString();
        }
    }
}