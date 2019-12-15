using Improbable.Gdk.Subscriptions;
using MDG.Invader.Components;
using MDG.Invader.Systems;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MDG.DTO;
using MDG.Invader.Monobehaviours.UserInterface;
using PointSchema = MdgSchema.Common.Point;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.Common;
using MDG.ScriptableObjects.Items;
using UnityEngine.EventSystems;
using MdgSchema.Common.Structure;
using MDG.Invader.Monobehaviours.Structures;
using MdgSchema.Common.Util;
using Improbable.Gdk.GameObjectCreation;
using System.Linq;
using MDG.Common.MonoBehaviours;
using Improbable.Gdk.Core;

namespace MDG.Invader.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
#pragma warning disable 649
        [Require] PointSchema.PointReader PointReader;

        [SerializeField]
        BuildMenu structureBuildMenu;
#pragma warning restore 649

        LinkedEntityComponent linkedEntityComponent;

        Dictionary<StructureType, StructureUIManager> TypeToOverlay;

        SelectionSystem selectionSystem;
        CommandGiveSystem commandGiveSystem;
        Camera inputCamera;
        ScriptableObjects.Structures.Structure selectedStructure;

        ShopBehaviour shopBehaviour;

        UnityClientConnector clientConnector;


        // Could just run search, again but dirty and this is also dirty, rethink this part later.
        EntityId clickedTerritory;

        public StructureUIManager GetStructureOverlay(StructureType structureType)
        {
            return TypeToOverlay[structureType];
        }

        // Start is called before the first frame update
        void Start()
        {
            clientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();

            inputCamera = GetComponent<Camera>();
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            commandGiveSystem = linkedEntityComponent.World.GetExistingSystem<CommandGiveSystem>();

            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;

            selectionSystem = linkedEntityComponent.World.GetExistingSystem<SelectionSystem>();
            selectionSystem.OnUnitSelectionUpdated += OnSelectionUpdated;

            structureBuildMenu.OnOptionSelected += SetupBuildCommand;
            structureBuildMenu.SetConfirmation(ConfirmStructurePurchase);
            structureBuildMenu.OnOptionConfirmed += OnStructureBuildRequested;
            structureBuildMenu.transform.parent.gameObject.SetActive(false);

            shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += GiveBuildCommand;
            LoadInStuctureOverlays();
        }

       
        private void OnSelectionUpdated(bool selectionMade)
        {
            structureBuildMenu.transform.parent.gameObject.SetActive(selectionMade);
        }


        private void LoadInStuctureOverlays()
        {
            TypeToOverlay = new Dictionary<StructureType, StructureUIManager>();

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

        private bool ConfirmStructurePurchase()
        {
            bool baseConditions = selectedStructure != null && Input.GetMouseButtonDown(1);
            Debug.Log("Getting here base conditions met " + baseConditions);
            if (!baseConditions)
            {
                return false;
            }

            switch (selectedStructure.StructureType) {

                case StructureType.Claiming:
                    List<SpatialOSEntity> territoryEntities = clientConnector.TerritoryEntities;
                    ClientGameObjectCreator clientGameObjectCreator = clientConnector.ClientGameObjectCreator;
                    SpatialOSEntity territoryEntity = territoryEntities.Find((entity) =>
                    {
                        GameObject linkedObject = clientGameObjectCreator.GetLinkedGameObjectById(entity.SpatialOSEntityId);
                        return (linkedObject != null 
                        && linkedObject.TryGetComponent(out ClickableMonobehaviour clickableMonobehaviour)
                        && clickableMonobehaviour.MouseOver);
                    });
                    Debug.Log("clicked on territory this frame " + territoryEntity.SpatialOSEntityId);
                    clickedTerritory = territoryEntity.SpatialOSEntityId;
                    return clickedTerritory.IsValid();
                case StructureType.Spawning:
                    Debug.Log("spawn structure");
                    return baseConditions;
                default:
                    return false;
            }
        }

        private void OnStructureBuildRequested(ScriptableObjects.Items.ShopItem obj)
        {
            shopBehaviour.TryPurchase(obj, linkedEntityComponent);
        }

        private void SetupBuildCommand(ScriptableObjects.Items.ShopItem shopItem)
        {
            selectedStructure = shopItem as ScriptableObjects.Structures.Structure;
        }

        private void GiveBuildCommand(ShopItem item, LinkedEntityComponent purchaser)
        {
            ScriptableObjects.Structures.Structure scriptableStructure = item as ScriptableObjects.Structures.Structure;
            Vector3 position = HelperFunctions.GetMousePosition(inputCamera);
            commandGiveSystem.GiveBuildCommand(new BuildCommand
            {
                buildLocation = new Vector3f(position.x, 20, position.z),
                structureType = scriptableStructure.StructureType,
                minDistanceToBuild = scriptableStructure.MinDistanceToBuild,
                structureId = new Improbable.Gdk.Core.EntityId(-1),
                constructionTime = scriptableStructure.ConstructionTime,
                territoryId =  clickedTerritory
            });
            clickedTerritory = new EntityId(-1);
        }

        void UpdateSelectionComponent(SelectionController.SelectionPayload payload)
        {
            // Maybe it's this lol.
            if (EventSystem.current.IsPointerOverGameObject())
            {
               return;
            }
            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity)) {
                float3 convertedStart = inputCamera.ScreenToWorldPoint(new Vector3(payload.startPosition.x, payload.startPosition.y, inputCamera.farClipPlane));
                float3 convertedEnd = inputCamera.ScreenToWorldPoint(new Vector3(payload.endPosition.x, payload.endPosition.y, inputCamera.farClipPlane));
                float3 convertedScale = inputCamera.ScreenToWorldPoint(payload.scale);
                // Need to check if clicked on UI vs game
                float3 botLeft = new float3(math.min(convertedStart.x, convertedEnd.x), math.min(convertedStart.z, convertedEnd.z), 0);
                float3 topRight = new float3(math.max(convertedStart.x, convertedEnd.x), math.max(convertedStart.z, convertedEnd.z), 0);
                if (payload.scale.magnitude < 5.0f)
                {
                    Debug.Log("increasing selection size.");
                    botLeft += new float3(-5, -5, 0) * (10 - payload.scale.magnitude) * .5f;
                    topRight += new float3(+5, +5, 0) * (10 - payload.scale.magnitude) * .5f;
                }

                selectionSystem.SetSelectionBounds(new SelectionSystem.SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight
                });
            }
        }
    }
}