using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG;
using MDG.Invader.Components;
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
        [Require] PointReader pointReader = null;
        ComponentUpdateSystem componentUpdateSystem;

        Text numberOfUnitsText;
        Text pointText;

        EntityQuery unitQuery;
        void Start()
        {
            pointReader.OnValueUpdate += UpdatePointText;
            // Setup for updating unit count.
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            unitQuery = linkedEntityComponent.Worker.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Unit.Component>(),
                ComponentType.ReadOnly<CommandListener>()
                );

            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
            numberOfUnitsText = GameObject.Find("UnitCountText").GetComponent<Text>();
            pointText = GameObject.Find("PointText").GetComponent<Text>();

            UnityClientConnector unityClientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            unityClientConnector.clientGameObjectCreator.OnEntityAdded += UpdateUnitCount;
            unityClientConnector.clientGameObjectCreator.OnEntityDeleted += UpdateUnitCount;

        }
        private void UpdatePointText(int pointValue)
        {
            pointText.text = pointValue.ToString();
        }

        private void UpdateUnitCount(EntityId entityId)
        {
            int newUnitCount = unitQuery.CalculateEntityCount();
            numberOfUnitsText.text = newUnitCount.ToString();
        }
    }
}