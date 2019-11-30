using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.DTO;
using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;


namespace MDG.Common.MonoBehaviours.Structures
{
    public interface IStructure
    {
        // Not shop item as not always applicable so bytes as job context.
        void Link(StructureBehaviour structureBehaviour);
        void StartJob(byte[] jobContext);
        void CompleteJob(byte[] jobData);
    }


    // Not all structures have multipe jobs as shop items, need to update this.
    [RequireComponent(typeof(ShopBehaviour))]
    // For invader specifically.. Too general and too grand.
    public class StructureBehaviour : MonoBehaviour
    {
        // All logic is simply fetching data, so it's not pure visual since doing I/O
        // If need be move to structure base class, need to think about how to
        public event Action<StructureSchema.BuildEventPayload> OnBuild;
        public event Action OnBuildComplete;
        public event Action<int, ShopItem, LinkedEntityComponent> OnJobStarted;
        public event Action<StructureSchema.JobRunEventPayload> OnJobRun;
        public event Action<int, byte[]> OnJobCompleted;
        public event Action<string> OnError;

        public int JobCapacity;
        ComponentUpdateSystem componentUpdateSystem;
        LinkedEntityComponent linkedEntityComponent;
        [Require] StructureSchema.StructureReader structureReader = null;

        int jobIndex;
        private ShopItem[] jobQueue;


        // Gotta figure out how to assign this, prob though zenject as it shuldn't care what value is.
        
        public IStructure ConcreteStructureBehaviour { set; get; }

        // Wierd dependancy if do inheritance, think structure
        public virtual void Start()
        {
            jobIndex = 0;
            jobQueue = new ShopItem[JobCapacity];
            ConcreteStructureBehaviour.Link(this);
            ShopBehaviour shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += StartJob;
            structureReader.OnConstructingUpdate += OnConstructingUpdate;
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
        }

        // Layered events but it's fine.
        private void OnConstructingUpdate(bool constructing)
        {
            if (!constructing){
                OnBuildComplete?.Invoke();
            }
        }

        // Need to set up a core UI error handler.
        public virtual void StartJob(ShopItem shopItem, LinkedEntityComponent purchaser)
        {
            if (jobIndex == 0 && jobQueue[0] == null)
            {
                OnError?.Invoke("Reached maximum job occupancy");
            }
            else
            {
                PurchasePayload purchasePayload = new PurchasePayload
                {
                    shopItem = shopItem,
                    purchaserId = purchaser.EntityId.Id
                };
                ConcreteStructureBehaviour.StartJob(Converters.SerializeArguments<PurchasePayload>(purchasePayload));
                OnJobStarted?.Invoke(jobIndex, shopItem, purchaser);
                ++jobIndex;
            }
        }

        private void Update()
        {

            #region Checking for Building and Job Events
            if (structureReader.Data.Constructing)
            {
                var buildEvents = componentUpdateSystem.GetEventsReceived<StructureSchema.Structure.Building.Event>(linkedEntityComponent.EntityId);
                for (int i = 0; i < buildEvents.Count; ++i)
                {
                    // I literally made both of them timers.
                    ref readonly var buildEvent = ref buildEvents[i];
                    float percentage = buildEvent.Event.Payload.BuildProgress / buildEvent.Event.Payload.EstimatedBuildCompletion;
                    OnBuild?.Invoke(buildEvent.Event.Payload);
                }
            }
            else
            {
                var jobProgressEvents = componentUpdateSystem.GetEventsReceived<StructureSchema.Structure.JobRunning.Event>(linkedEntityComponent.EntityId);
                if (jobProgressEvents.Count > 0)
                {
                    ref readonly var jobProgressEvent = ref jobProgressEvents[0];
                    float pct = jobProgressEvent.Event.Payload.JobProgress / jobProgressEvent.Event.Payload.EstimatedJobCompletion;
                    OnJobRun?.Invoke(jobProgressEvent.Event.Payload);
                }

                var jobCompleteEvents = componentUpdateSystem.GetEventsReceived<StructureSchema.Structure.JobComplete.Event>(linkedEntityComponent.EntityId);
                if (jobCompleteEvents.Count > 0)
                {
                    ref readonly var jobCompleteEvent = ref jobCompleteEvents[0];
                    ConcreteStructureBehaviour.CompleteJob(jobCompleteEvent.Event.Payload.JobData);
                    OnJobCompleted?.Invoke(jobIndex, jobCompleteEvent.Event.Payload.JobData);
                    jobIndex = (jobIndex + 1) % JobCapacity;
                }
            }
            #endregion
        }
    }
}