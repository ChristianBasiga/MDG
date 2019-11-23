using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.ScriptableObjects.Items;
using MDG.DTO;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using MDG.Common;
using StructureSchema = MdgSchema.Common.Structure;
using TerritorySchema = MdgSchema.Game.Territory;
using MdgSchema.Units;
using MDG.Common.Systems.Spawn;
using MDG.Common.MonoBehaviours.Structures;
using Unity.Entities;

namespace MDG.Invader.Monobehaviours.Structures
{
    // maybe do make this  amono.
    public class ClaimingStructure : MonoBehaviour, IStructure
    {
        LinkedEntityComponent linkedStructure;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        Image claimProgressBar;


        private void Start()
        {
            claimProgressBar = transform.Find("ClaimProgress").GetComponent<Image>();
        }
        public void Link(StructureBehaviour structureBehaviour)
        {
            LinkedEntityComponent linkedEntityComponent = structureBehaviour.GetComponent<LinkedEntityComponent>();
            linkedStructure = linkedEntityComponent;
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
            commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();
            structureBehaviour.OnJobRun += UpdateClaimProgress;
            structureBehaviour.OnBuildComplete += OnBuildComplete;
        }

        private void OnBuildComplete()
        {
            // This is fine.
            EntityManager entityManager = linkedStructure.World.EntityManager;
            WorkerSystem workerSystem = linkedStructure.World.GetExistingSystem<WorkerSystem>();
            workerSystem.TryGetEntity(linkedStructure.EntityId, out Entity entity);
            
            StructureSchema.ClaimStructure.Component claimStructureComponent = entityManager.GetComponentData<StructureSchema.ClaimStructure.Component>(entity);
            StructureSchema.StructureMetadata.Component structureMetadata = entityManager.GetComponentData<StructureSchema.StructureMetadata.Component>(entity);

            Debug.Log($"Beginning claim on {claimStructureComponent.TerritoryClaiming}");

            ClaimConfig claimConfig = new ClaimConfig
            {
                territoryId = claimStructureComponent.TerritoryClaiming,
                constructionTime = structureMetadata.ConstructionTime
            };
            StartJob(Converters.SerializeArguments<ClaimConfig>(claimConfig));
        }
       
        // I've tested none of this and don't even remember at what point I'm at.
        public void StartJob(byte[] jobContext)
        {
            ClaimConfig claimConfig = Converters.DeserializeArguments<ClaimConfig>(jobContext);
            commandSystem.SendCommand(new StructureSchema.Structure.StartJob.Request
            {
                Payload = new StructureSchema.JobRequestPayload
                {
                    JobData = jobContext,
                    EstimatedJobCompletion = claimConfig.constructionTime
                },
                TargetEntityId = linkedStructure.EntityId
            });
        }

        public void UpdateClaimProgress(StructureSchema.JobRunEventPayload jobRunEvent)
        {
            // Do other things alongside the claim progress bar.
            linkedStructure.StartCoroutine(HelperFunctions.UpdateFill(claimProgressBar, jobRunEvent.JobProgress / jobRunEvent.EstimatedJobCompletion));
        }

        public void CompleteJob(byte[] jobData)
        {
            ClaimConfig claimConfig = Converters.DeserializeArguments<ClaimConfig>(jobData);
             commandSystem.SendCommand(new TerritorySchema.TerritoryStatus.UpdateClaim.Request{

                Payload = new TerritorySchema.UpdateTerritoryStatusRequest
                {
                    Status = TerritorySchema.TerritoryStatusTypes.Claimed
                },
                TargetEntityId = claimConfig.territoryId
             });

            claimProgressBar.gameObject.SetActive(false);
        }
    }
}