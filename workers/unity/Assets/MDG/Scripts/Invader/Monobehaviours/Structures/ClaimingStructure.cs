using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.DTO;
using Unity.Entities;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;
using TerritorySchema = MdgSchema.Game.Territory;

namespace MDG.Invader.Monobehaviours.Structures
{
    // maybe do make this  amono.
    public class ClaimingStructure : MonoBehaviour, IStructure
    {
        LinkedEntityComponent linkedStructure;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        EntityId territoryClaiming;

        private void Start()
        {
        }
        public void Link(StructureBehaviour structureBehaviour)
        {
            LinkedEntityComponent linkedEntityComponent = structureBehaviour.GetComponent<LinkedEntityComponent>();
            linkedStructure = linkedEntityComponent;
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
            commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();
            structureBehaviour.OnBuildComplete += OnBuildComplete;
        }

        private void OnBuildComplete()
        {
            EntityManager entityManager = linkedStructure.World.EntityManager;
            WorkerSystem workerSystem = linkedStructure.World.GetExistingSystem<WorkerSystem>();
            workerSystem.TryGetEntity(linkedStructure.EntityId, out Entity entity);
            
            StructureSchema.ClaimStructure.Component claimStructureComponent = entityManager.GetComponentData<StructureSchema.ClaimStructure.Component>(entity);
            territoryClaiming = claimStructureComponent.TerritoryClaiming;
            StructureSchema.StructureMetadata.Component structureMetadata = entityManager.GetComponentData<StructureSchema.StructureMetadata.Component>(entity);

            Debug.Log($"Beginning claim on {claimStructureComponent.TerritoryClaiming}");

            ClaimConfig claimConfig = new ClaimConfig
            {
                territoryId = claimStructureComponent.TerritoryClaiming.Id,
                constructionTime = structureMetadata.ConstructionTime
            };
            commandSystem.SendCommand(new TerritorySchema.TerritoryStatus.UpdateClaim.Request
            {
                TargetEntityId = territoryClaiming,
                Payload = new TerritorySchema.UpdateTerritoryStatusRequest
                {
                    Status = TerritorySchema.TerritoryStatusTypes.Claiming
                }
            }, entity);
        }


        private void OnDestroy()
        {
            commandSystem.SendCommand(new TerritorySchema.TerritoryStatus.UpdateClaim.Request
            {
                TargetEntityId = territoryClaiming,
                Payload = new TerritorySchema.UpdateTerritoryStatusRequest
                {
                    Status = TerritorySchema.TerritoryStatusTypes.Released
                }
            });
        }
        // I've tested none of this and don't even remember at what point I'm at.
        public void StartJob(byte[] jobContext)
        {
            /*
            ClaimConfig claimConfig = Converters.DeserializeArguments<ClaimConfig>(jobContext);
            commandSystem.SendCommand(new StructureSchema.Structure.StartJob.Request
            {
                Payload = new StructureSchema.JobRequestPayload
                {
                    JobData = jobContext,
                    EstimatedJobCompletion = claimConfig.constructionTime
                },
                TargetEntityId = linkedStructure.EntityId
            });*/
        }

        public StructureSchema.StructureType GetStructureType()
        {
            return StructureSchema.StructureType.Claiming;
        }

        public void CompleteJob(byte[] jobData)
        {
            // Jobs for claim will be repairs and upgrades, do later.
        }
    }
}