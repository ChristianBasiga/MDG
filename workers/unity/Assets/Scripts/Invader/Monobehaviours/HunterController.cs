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

namespace MDG.Invader.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
        [Require] PointSchema.PointReader PointReader;
        LinkedEntityComponent linkedEntityComponent;


        CommandGiveSystem commandGiveSystem;
        Camera inputCamera;
        public InvaderStructureConfig SelectedStructure {private set; get;}

        ShopBehaviour shopBehaviour;

        [SerializeField]
        BuildMenu structureBuildMenu;

        // Start is called before the first frame update
        void Start()
        {
            inputCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            commandGiveSystem = linkedEntityComponent.World.GetExistingSystem<CommandGiveSystem>();

            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;

          
            structureBuildMenu.OnOptionSelected += SetupBuildCommand;
            structureBuildMenu.SetConfirmation(ConfirmStructurePurchase);
            structureBuildMenu.OnOptionConfirmed += OnStructureBuildRequested;

            shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += GiveBuildCommand;
        }


        private bool ConfirmStructurePurchase()
        {
            return SelectedStructure != null && Input.GetMouseButtonDown(0);
        }

        private void OnStructureBuildRequested(ScriptableObjects.Items.ShopItem obj)
        {
            Debug.Log("Trying purchase");
            shopBehaviour.TryPurchase(obj, linkedEntityComponent);
        }

        private void SetupBuildCommand(ScriptableObjects.Items.ShopItem shopItem)
        {
            ScriptableObjects.Structures.Structure scriptableStructure = shopItem as ScriptableObjects.Structures.Structure;
            // After this I want to get all selected units.
            InvaderStructureConfig structureConfig = new InvaderStructureConfig
            {
                constructionTime = scriptableStructure.ConstructionTime,
                prefabName = scriptableStructure.PrefabPath,
                structureType = scriptableStructure.StructureType,
                WorkersRequired = scriptableStructure.WorkersRequired
            };
            // Well okay, so since build command is really just a broadcast now irrelvant to right click.
            SelectedStructure = structureConfig;

            Debug.Log("setting up build command");
        }

        private void GiveBuildCommand(ShopItem item, LinkedEntityComponent purchaser)
        {
            // Send build request. Need to create end point for this first.
            // Actually could just reference system since both on client.
            Debug.Log("giving build command");

            var selectedStructure = SelectedStructure;
            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            commandGiveSystem.GiveBuildCommand(new BuildCommand
            {
                buildLocation = new Improbable.Vector3f(position.x, 15, position.z),
                structureType = selectedStructure.structureType,
            });
            SelectedStructure = null;
             
        }

        void UpdateSelectionComponent(SelectionController.SelectionPayload payload)
        {
            if (payload.scale.magnitude < SelectionSystem.MinSelectionSize  && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Don't send selecion");
                return;
            }

            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity)) {
                float3 convertedStart = inputCamera.ScreenToWorldPoint(payload.startPosition);
                float3 convertedEnd = inputCamera.ScreenToWorldPoint(payload.endPosition);
                float3 convertedScale = inputCamera.ScreenToWorldPoint(payload.scale);
                // Need to check if clicked on UI vs game
                linkedEntityComponent.World.EntityManager.AddComponentData(entity, new Selection { StartPosition = convertedStart, Scale = convertedScale, EndPosition = convertedEnd});
            }
        }
    }
}