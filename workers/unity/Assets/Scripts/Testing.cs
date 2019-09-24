using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using MDG.Common.Components;

public class Testing : MonoBehaviour
{
    LinkedEntityComponent linkedEntityComponent;    
    void Start()
    {
        linkedEntityComponent = GetComponent<LinkedEntityComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            WorkerSystem workerSystem = linkedEntityComponent.Worker;

            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                Debug.LogError("here");
                workerSystem.EntityManager.AddComponentData(entity, new PendingInventoryAddition
                {
                    Count = 1,
                    ItemId = 1
                });
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            WorkerSystem workerSystem = linkedEntityComponent.Worker;

            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity))
            {
                workerSystem.EntityManager.AddComponentData(entity, new PendingInventoryRemoval
                {
                   InventoryIndex = 0
                });
            }
        }
    }
}
