using Improbable.Gdk.Subscriptions;
using MDG.Hunter.Components;
using MDG.Hunter.Systems;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MDG.Hunter.Monobehaviours {

    public class HunterController : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;
        // Start is called before the first frame update
        void Start()
        {
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionEnd += UpdateSelectionComponent;
        }

        void UpdateSelectionComponent(SelectionController.SelectionPayload payload)
        {
            if (linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity)) {

                float3 convertedStart = Camera.main.ScreenToWorldPoint(payload.startPosition);
                float3 convertedEnd = Camera.main.ScreenToWorldPoint(payload.endPosition);
                float3 convertedScale = Camera.main.ScreenToWorldPoint(payload.scale);
                linkedEntityComponent.World.EntityManager.AddComponentData(entity, new Selection { StartPosition = convertedStart, Scale = convertedScale, EndPosition = convertedEnd});
            }
        }
    }
}