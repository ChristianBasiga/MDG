using Improbable.Gdk.Subscriptions;
using MDG.Invader.Components;
using MDG.Invader.Systems;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MDG.Invader.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;
        Camera inputCamera;
        // Start is called before the first frame update
        void Start()
        {
            inputCamera = transform.GetChild(0).GetComponent<Camera>();
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;
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