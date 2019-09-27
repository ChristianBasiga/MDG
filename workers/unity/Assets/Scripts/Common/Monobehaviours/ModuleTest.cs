using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using MDG.Common.Components;

namespace MDG.Common.MonoBehaviours.Inventory
{
    public class ModuleTest : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;

        public bool AddedItemToInventory
        {
            private set; get;
        }

        public bool RemovedItemFromInventory
        {
            private set; get;
        }

        public bool AddedItemAfterRemovedFromInventory
        {
            private set; get;
        }

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
                    workerSystem.EntityManager.AddComponentData(entity, new PendingInventoryAddition
                    {
                        Count = 1,
                        ItemId = 1
                    });

                    AddedItemToInventory = true;
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
}