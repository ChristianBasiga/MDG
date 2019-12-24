using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.DTO;
using MDG.Invader.Monobehaviours.UserInterface;
using MDG.Invader.Systems;
using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;


namespace MDG.Invader.Monobehaviours.Structures
{
    public interface IStructure
    {
        // Not shop item as not always applicable so bytes as job context.
        void Link(StructureBehaviour structureBehaviour);
        void StartJob(byte[] jobContext);
        void CompleteJob(byte[] jobData);
        StructureSchema.StructureType GetStructureType();
    }


    public class StructureBehaviour : MonoBehaviour
    {
        // All logic is simply fetching data, so it's not pure visual since doing I/O
        // If need be move to structure base class, need to think about how to
        public event Action<StructureSchema.BuildEventPayload> OnBuild;
        public event Action OnBuildComplete;
        public event Action<int, ShopItem, LinkedEntityComponent> OnJobStarted;
        public event Action<int, StructureSchema.JobRunEventPayload> OnJobRun;
        public event Action<int, byte[]> OnJobCompleted;
        public event Action<string> OnError;

        public int JobCapacity;
        ComponentUpdateSystem componentUpdateSystem;
        LinkedEntityComponent linkedEntityComponent;
        LinkedEntityComponent invaderLink;

        [Require] StructureSchema.StructureReader structureReader = null;

        int nextJobIndex;
        int currentlyRunningJob;

        private ShopItem[] jobQueue;
        StructureUIManager structureUIManager;

        public IStructure ConcreteStructureBehaviour { set; get; }

        private void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                structureUIManager.SetStructure(this);
                structureUIManager.SetJobs(jobQueue);
                structureUIManager.gameObject.SetActive(true);
            }
        }

        // Wierd dependancy if do inheritance, think structure
        public virtual void Start()
        {
            nextJobIndex = 0;
            currentlyRunningJob = 0;
            jobQueue = new ShopItem[JobCapacity];
            // This is crucial lol. How do I get ref to build menu of someting I don't haveeeee
            ShopBehaviour shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += StartJob;

            structureReader.OnBuildingEvent += OnUpdateBuilding;
            structureReader.OnConstructingUpdate += OnConstructingUpdate;

            structureReader.OnJobRunningEvent += OnUpdateJob;
            structureReader.OnJobCompleteEvent += OnJobComplete;

            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();


            ConcreteStructureBehaviour = GetComponent<IStructure>();
            ConcreteStructureBehaviour.Link(this);

            invaderLink = linkedEntityComponent.World.GetExistingSystem<CommandGiveSystem>().InvaderLink;
            // Should get from global state somewhere, not storing in invade hud, that's silly.
            structureUIManager = invaderLink.GetComponent<InvaderHud>().GetStructureOverlay(ConcreteStructureBehaviour.GetStructureType());
            structureUIManager.buildMenu.OnOptionConfirmed += BuildMenu_OnOptionSelected;
        }

        private void Update()
        {
            // Need to create UI stack.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                structureUIManager.gameObject.SetActive(false);
            }
        }

        private void OnJobComplete(StructureSchema.JobCompleteEventPayload jobCompleteEventPayload)
        {
            ConcreteStructureBehaviour.CompleteJob(jobCompleteEventPayload.JobData);
            OnJobCompleted?.Invoke(currentlyRunningJob, jobCompleteEventPayload.JobData);
            jobQueue[currentlyRunningJob] = null;
            currentlyRunningJob = nextJobIndex;
        }

        private void OnUpdateJob(StructureSchema.JobRunEventPayload jobRunEventPayload)
        {
            OnJobRun?.Invoke(currentlyRunningJob, jobRunEventPayload);
        }

        private void OnUpdateBuilding(StructureSchema.BuildEventPayload buildEventPayload)
        {
            OnBuild?.Invoke(buildEventPayload);
        }

        private void BuildMenu_OnOptionSelected(ShopItem obj)
        {
            ShopBehaviour shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.TryPurchase(obj, invaderLink);
        }

        // Layered events but it's fine.
        private void OnConstructingUpdate(bool constructing)
        {
            if (!constructing){
                Debug.Log("No longer constructing");
                OnBuildComplete?.Invoke();
            }
        }

        // Need to set up a core UI error handler.
        public virtual void StartJob(ShopItem shopItem, LinkedEntityComponent purchaser)
        {
            ShopItemDto shopItemDto = Converters.ShopItemToDto(shopItem);
            PurchasePayload purchasePayload = new PurchasePayload
            {
                shopItem = shopItemDto,
                purchaserId = purchaser.EntityId.Id
            };

            if (jobQueue[nextJobIndex] != null)
            {
                OnError?.Invoke("Job Queue is Full");
            }
            else
            {
                if (jobQueue[0] == null)
                {
                    nextJobIndex = 0;
                }
                if (jobQueue[currentlyRunningJob] == null)
                {
                    ConcreteStructureBehaviour.StartJob(Converters.SerializeArguments(purchasePayload));
                    currentlyRunningJob = nextJobIndex;
                }
                else
                {
                    Debug.Log("Job is busy. Queuing this job up");
                    StartCoroutine(QueueNextJob(shopItem, purchaser, purchasePayload, currentlyRunningJob));
                }
                OnJobStarted?.Invoke(nextJobIndex, shopItem, purchaser);
                // Setting of image is  
                jobQueue[nextJobIndex++] = shopItem;
                nextJobIndex = nextJobIndex % jobQueue.Length;
            }
        }

        IEnumerator QueueNextJob(ShopItem shopItem, LinkedEntityComponent purchaser, PurchasePayload purchasePayload, int busyJobIndex)
        {
            yield return new WaitWhile(() => jobQueue[busyJobIndex] != null);
            currentlyRunningJob = (busyJobIndex + 1) % jobQueue.Length;
            OnJobStarted?.Invoke(currentlyRunningJob, shopItem, purchaser);
            ConcreteStructureBehaviour.StartJob(Converters.SerializeArguments(purchasePayload));
        }
    }
}