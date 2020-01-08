using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Invader.Components;
using MDG.Invader.Monobehaviours.Structures;
using MdgSchema.Common.Structure;
using MdgSchema.Units;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MDG.Invader.Monobehaviours.UserInterface
{
    public class InvaderHud : MonoBehaviour
    {
        public BuildMenu structureBuildMenu;

        [SerializeField]
        Text numberOfUnitsText;

        [SerializeField]
        Text pointText;

        Dictionary<StructureType, StructureUIManager> TypeToOverlay;
     

        EntityQuery unitQuery;
        void Start()
        {
            // Setup for updating unit count.
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            unitQuery = linkedEntityComponent.Worker.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Unit.Component>(),
                ComponentType.ReadOnly<CommandListener>()
                );

            UnityClientConnector unityClientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            unityClientConnector.ClientGameObjectCreator.OnEntityAdded += (spatialEntity) => { UpdateUnitCount(spatialEntity.SpatialOSEntityId); };
            unityClientConnector.ClientGameObjectCreator.OnEntityDeleted += UpdateUnitCount;
            numberOfUnitsText.text = unitQuery.CalculateEntityCount().ToString();
            structureBuildMenu.transform.parent.gameObject.SetActive(false);
            LoadInStuctureOverlays();

        }

        public StructureUIManager GetStructureOverlay(StructureType structureType)
        {
            return TypeToOverlay[structureType];
        }


        private void LoadInStuctureOverlays()
        {
            TypeToOverlay = new Dictionary<StructureType, StructureUIManager>();

            // Will async load these to not block later, for now there isn't enough to mater
            object[] overlays = Resources.LoadAll("UserInterface/StructureOverlays/");
            int length = overlays.Length;
            for (int i = 0; i < length; ++i)
            {
                GameObject gameObject = overlays[i] as GameObject;
                GameObject cloned = Instantiate(gameObject);
                StructureUIManager structureUIManager = cloned.GetComponent<StructureUIManager>();
                TypeToOverlay.Add(structureUIManager.StructureType, structureUIManager);
                cloned.SetActive(false);
            }
        }


        public void ToggleBuildMenu(bool toggle)
        {
            structureBuildMenu.transform.parent.gameObject.SetActive(toggle);
        }

        public void UpdatePointText(int pointValue)
        {
            pointText.text = pointValue.ToString();
        }

        public void UpdateUnitCount(EntityId entityId)
        {
            int newUnitCount = unitQuery.CalculateEntityCount();
            numberOfUnitsText.text = newUnitCount.ToString();
        }
    }
}