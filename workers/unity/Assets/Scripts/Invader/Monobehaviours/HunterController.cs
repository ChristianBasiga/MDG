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

namespace MDG.Invader.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
        [Require] PointSchema.PointReader PointReader;
        LinkedEntityComponent linkedEntityComponent;
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

            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;

            shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += TryBuildCommand;
            // Need to determine location placing structure.
            // So there is selected and confirmed. Gotta update that
            structureBuildMenu.OnOptionSelected += OnStructureBuildRequested;
        }

        // Unless I make hunter object also have shop behaviour
        private void OnStructureBuildRequested(ScriptableObjects.Items.ShopItem obj)
        {
            shopBehaviour.TryPurchase(obj, linkedEntityComponent);
        }

        private void TryBuildCommand(ScriptableObjects.Items.ShopItem shopItem, LinkedEntityComponent purchaser)
        {
            if (purchaser.EntityId.Equals(linkedEntityComponent.EntityId))
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
            }
        }

        void UpdateSelectionComponent(SelectionController.SelectionPayload payload)
        {
            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity)) {
                float3 convertedStart = inputCamera.ScreenToWorldPoint(payload.startPosition);
                float3 convertedEnd = inputCamera.ScreenToWorldPoint(payload.endPosition);
                float3 convertedScale = inputCamera.ScreenToWorldPoint(payload.scale);
                linkedEntityComponent.World.EntityManager.AddComponentData(entity, new Selection { StartPosition = convertedStart, Scale = convertedScale, EndPosition = convertedEnd});
            }
        }
    }
}