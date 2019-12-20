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
using MDG.Invader.Monobehaviours.InputProcessors;
using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours.Synchronizers;

namespace MDG.Invader.Monobehaviours {

    public class InvaderSynchronizer : MonoBehaviour, IPlayerSynchronizer
    {

        public InvaderHud InvaderHud { private set; get; }
        public UnityClientConnector ClientWorker { private set; get; }

        LinkedEntityComponent linkedEntityComponent;

        List<IProcessInput> inputProcessors;
        SelectionSystem selectionSystem;
        CommandGiveSystem commandGiveSystem;
        Camera inputCamera;
        ScriptableObjects.Structures.Structure selectedStructure;

        ShopBehaviour shopBehaviour;
        // Could just run search, again but dirty and this is also dirty, rethink this part later.
        EntityId clickedTerritory;

      
        // Start is called before the first frame update
        void Start()
        {
            InvaderHud = GetComponent<InvaderHud>();
            
            inputCamera = GetComponent<Camera>();
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            commandGiveSystem = linkedEntityComponent.World.GetExistingSystem<CommandGiveSystem>();

            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;

            selectionSystem = linkedEntityComponent.World.GetExistingSystem<SelectionSystem>();
            selectionSystem.OnUnitSelectionUpdated += InvaderHud.ToggleBuildMenu;

            InvaderHud.structureBuildMenu.OnOptionSelected += SetupBuildCommand;
            InvaderHud.structureBuildMenu.SetConfirmation(ConfirmStructurePurchase);
            InvaderHud.structureBuildMenu.OnOptionConfirmed += OnStructureBuildRequested;

            shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += GiveBuildCommand;
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
                    List<SpatialOSEntity> territoryEntities = ClientWorker.TerritoryEntities;
                    ClientGameObjectCreator clientGameObjectCreator = ClientWorker.ClientGameObjectCreator;
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
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Stuck here???");
               return;
            }

            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity)) {
                float3 convertedStart = inputCamera.ScreenToWorldPoint(new Vector3(payload.startPosition.x, payload.startPosition.y, inputCamera.farClipPlane));
                float3 convertedEnd = inputCamera.ScreenToWorldPoint(new Vector3(payload.endPosition.x, payload.endPosition.y, inputCamera.farClipPlane));
                float3 convertedScale = inputCamera.ScreenToWorldPoint(payload.scale);
                float3 botLeft = new float3(math.min(convertedStart.x, convertedEnd.x), math.min(convertedStart.z, convertedEnd.z), 0);
                float3 topRight = new float3(math.max(convertedStart.x, convertedEnd.x), math.max(convertedStart.z, convertedEnd.z), 0);
                selectionSystem.SetSelectionBounds(new SelectionSystem.SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight
                });
            }
        }

        public void LinkClientWorker(UnityClientConnector unityClientConnector)
        {
            ClientWorker = unityClientConnector;

            if (unityClientConnector.TryGetComponent(out GameStatusSynchronizer gameStatusSynchronizer))
            {
                if (TryGetComponent(out InputProcessorManager inputProcessorManager))
                {
                    inputProcessorManager.SetSynchronizer(gameStatusSynchronizer);
                }
                else
                {
                    Debug.LogError("Player synchronizer is missing input processor manager");
                }
            }
            else
            {
                Debug.LogError("Client worker is missing game status synchronizer.");
            }
        }
    }
}