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
using MdgSchema.Units;
using MDG.Common.Systems.Spawn;
using MDG.Common.MonoBehaviours.Structures;

// Need to change where this goes since longer monobehaviour.
namespace MDG.Invader.Monobehaviours.Structures
{
    public class UnitSpawnerStructure : IStructure
    {
        LinkedEntityComponent linkedStructure;
        SpawnRequestSystem spawnRequestSystem;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        public Vector3 spawnOffset;

        public void Link(StructureBehaviour structureBehaviour)
        {
            LinkedEntityComponent linkedEntityComponent = structureBehaviour.GetComponent<LinkedEntityComponent>();
            linkedStructure = linkedEntityComponent;
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
        }
        // Create abstract class with startjob base sending StartJobRequest.
        // Should be in structure interface called start job as common argumens.
        public void StartJob(ScriptableObjects.Items.ShopItem shopItem, LinkedEntityComponent purchaser)
        {
            if (shopItem.shopItemType != ScriptableObjects.Constants.ShopItemType.Unit)
            {
                return;
            }

            ShopUnit shopUnit = shopItem as ShopUnit;
            // So send run job requests with spawn request as serialzied payload.
            UnitConfig unitConfig = new UnitConfig
            {
                ownerId = purchaser.EntityId.Id,
                position = HelperFunctions.Vector3fFromUnityVector(linkedStructure.transform.position + spawnOffset),
                unitType = shopUnit.UnitType
            };

            byte[] jobPayload = Converters.SerializeArguments<UnitConfig>(unitConfig);
            // Don't care about the response right now.
            commandSystem.SendCommand(new StructureSchema.Structure.StartJob.Request
            {
                Payload = new StructureSchema.JobRequestPayload
                {
                    JobData = Converters.SerializeArguments<UnitConfig>(unitConfig),
                    EstimatedJobCompletion = shopUnit.ConstructTime,
                },
                TargetEntityId = linkedStructure.EntityId
            });
        }

        public void CompleteJob(byte[] jobData)
        {
            // Will be removed when I include spawnPos in the byte payload for spawnMetaData.
            UnitConfig unitConfig = Converters.DeserializeArguments<UnitConfig>(jobData);
            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Count = 1,
                Position = unitConfig.position
            }, OnUnitSuccessfullySpawned, jobData);
        }

        private void OnUnitSuccessfullySpawned(EntityId entityId)
        {

        }
    }
}