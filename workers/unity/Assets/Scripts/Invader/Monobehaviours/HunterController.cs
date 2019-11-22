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
namespace MDG.Invader.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;
        Camera inputCamera;
        public StructureConfig SelectedStructure {private set; get;}

        Dictionary<string, StructureConfig> structureSelection;

        // Start is called before the first frame update
        void Start()
        {
            inputCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;
        }


        // Buttons will call this. Button the structure are attached to either pass themselves or string.
        // maybe instead of dictionary just pass structure config itself.
        void OnSelectStructureToBuild(string structureName){

            if (structureSelection.TryGetValue(structureName, out StructureConfig structureConfig)){
                Debug.Log($"Selected structure {structureName}");
                this.SelectedStructure = structureConfig;
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