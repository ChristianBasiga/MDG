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
using TerritorySchema = MdgSchema.Game.Territroy;
using MdgSchema.Units;
using MDG.Common.Systems.Spawn;
using MDG.Common.MonoBehaviours.Structures;

namespace MDG.Invader.Monobehaviours.Structures
{
    public class ClaimingStructure : IStructure
    {
        LinkedEntityComponent linkedStructure;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        // For knowing territory claiming.
        [Require] StructureSchema.ClaimStructureReader claimStructureReader;
        Image claimProgressBar;

        int claimRequestId = -1;
        public void Link(StructureBehaviour structureBehaviour)
        {
            LinkedEntityComponent linkedEntityComponent = structureBehaviour.GetComponent<LinkedEntityComponent>();
            linkedStructure = linkedEntityComponent;
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
            structureBehaviour.OnJobRun += UpdateClaimProgress;
            structureBehaviour.OnBuildComplete += OnBuildComplete;
        }

        private void OnBuildComplete()
        {
            // This is fine.
            Debug.Log($"Beginning claim on {claimStructureReader.Data.TerritoryClaiming}");
            ClaimConfig claimConfig = new ClaimConfig{
                territoryId = claimStructureReader.Data.TerritoryClaiming
            };
            StartJob(Converters.SerializeArguments<ClaimConfig>(claimConfig));
        }
        // Create abstract class with startjob base sending StartJobRequest.
        // Should be in structure interface called start job as common argumens.
        // I don't think shop item would work makes no sense in context of claiming job.
        public void StartJob(byte[] jobContext)
        {
            // No need to deserialize it. Maybe to store which territorty trying to claim for soem reason.
           // ClaimConfig claimConfig = Converters.DeserializeArguments<ClaimConfig>(jobContext);

            // Don't care about the response right now.
            commandSystem.SendCommand(new StructureSchema.Structure.StartJob.Request
            {
                Payload = new StructureSchema.JobRequestPayload
                {
                    JobData = jobContext
                    EstimatedJobCompletion = shopUnit.ConstructTime,
                },
                TargetEntityId = linkedStructure.EntityId
            });
        }

        public void UpdateClaimProgress(StructureSchema.JobRunEventPayload jobRunEvent)
        {
            // Do other things alongside the claim progress bar.
            StartCoroutine(HelperFunctions.UpdateFill(claimProgressBar, jobRunEvent.JobProgress / jobRunEvent.EstimatedJobCompletion));
        }

        // This fine, then will have reader on claiming structure that once claimed is true update UI.
        public void CompleteJob(byte[] jobData)
        {
            // Will be removed when I include spawnPos in the byte payload for spawnMetaData.
            // Tbh don't even need to deserialize since have the reader. But good for adding more data.
            ClaimConfig claimConfig = Converters.DeserializeArguments<ClaimConfig>(jobData);
            claimRequestId = commandSystem.SendCommand(new TerritorySchema.TerritoryStatus.UpdateClaim.Request{

                Payload = TerritorySchema.TerritoryStatusTypes.Claimed
            }, claimConfig.territoryId);

            claimProgressBar.gameObject.SetActive(false);
        }
    }
}