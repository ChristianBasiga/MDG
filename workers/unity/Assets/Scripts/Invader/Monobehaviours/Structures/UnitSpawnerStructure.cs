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
using MDG.ScriptableObjects.Units;
using System.Linq;

// Need to change where this goes since longer monobehaviour.
namespace MDG.Invader.Monobehaviours.Structures
{
    public class UnitSpawnerStructure : MonoBehaviour, IStructure
    {

        const string StoredUnitInfoPath = "ScriptableObjects/Units";

        [Require] StructureSchema.StructureCommandSender structureCommandSender;
        LinkedEntityComponent linkedStructure;
        SpawnRequestSystem spawnRequestSystem;
        public Vector3 spawnOffset;


        Dictionary<UnitTypes, InvaderUnit> InvaderUnitScriptableObjects;

        StructureUIManager structureUIManager;


        
        private void Start()
        {
            string[] names = System.Enum.GetNames(typeof(UnitTypes));
            InvaderUnitScriptableObjects = new Dictionary<UnitTypes, InvaderUnit>();
            for (int i = 0; i < names.Length; ++i)
            {
                var name = names[i];
                Debug.Log(name);
                // If promise goes out of scope, does the on complete still happen?
                ResourceRequest promise = Resources.LoadAsync($"{StoredUnitInfoPath}/{name}Unit");
                promise.completed += (AsyncOperation _) =>
                {
                    if (promise.asset != null)
                    {
                        UnitTypes unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), name);
                        InvaderUnitScriptableObjects.Add(unitType, promise.asset as InvaderUnit);
                    }
                };
            }
        }

       

        public StructureSchema.StructureType GetStructureType()
        {
            
            return StructureSchema.StructureType.Spawning;
        }

        public void Link(StructureBehaviour structureBehaviour)
        {
            LinkedEntityComponent linkedEntityComponent = structureBehaviour.GetComponent<LinkedEntityComponent>();
            linkedStructure = linkedEntityComponent;
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
        }
        // Create abstract class with startjob base sending StartJobRequest.
        // Should be in structure interface called start job as common argumens.
        public void StartJob(byte[] jobContext)
        {
            PurchasePayload purchasePayload = Converters.DeserializeArguments<PurchasePayload>(jobContext);
            // Hmm
            ShopUnitDto shopUnit = purchasePayload.shopItem as ShopUnitDto;

            // Should set combat stats too, etc. So from shop unit to load specific invader units.
            // So send run job requests with spawn request as serialzied payload.
            UnitConfig unitConfig = new UnitConfig
            {
                ownerId = purchasePayload.purchaserId,
                position = HelperFunctions.Vector3fFromUnityVector(linkedStructure.transform.position + spawnOffset),
                unitType = shopUnit.unitType,
            };

            Debug.Log("Shop unit construction time " + shopUnit.constructionTime);

            byte[] jobPayload = Converters.SerializeArguments<UnitConfig>(unitConfig);
            // Don't care about the response right now.

            Debug.Log("Starting job for targe entityid " + linkedStructure.EntityId);

            // Sending commands like this is  abit rough honestly.
            // Maybe queue up it some other way. Yeah, had to force it to update via attribute, if this was in system would prob be good.
            structureCommandSender.SendStartJobCommand(new StructureSchema.Structure.StartJob.Request
            {
                Payload = new StructureSchema.JobRequestPayload
                {
                    JobData = Converters.SerializeArguments<UnitConfig>(unitConfig),
                    EstimatedJobCompletion = shopUnit.constructionTime,
                },
                TargetEntityId = linkedStructure.EntityId
            }, (response)=> {

                switch (response.StatusCode)
                {
                    case Improbable.Worker.CInterop.StatusCode.Timeout:
                        Debug.Log("Timed out");
                        break;
                    case Improbable.Worker.CInterop.StatusCode.NotFound:
                        Debug.Log("Not found");
                        break;
                }
                Debug.Log("job started");
            });
        }

        public void CompleteJob(byte[] jobData)
        {
            // Will be removed when I include spawnPos in the byte payload for spawnMetaData.
            UnitConfig unitConfig = Converters.DeserializeArguments<UnitConfig>(jobData);
            //Todo: Debug how it got to 40, like it added y of structure pos.
            unitConfig.position.Y = 20;

            // Need to delay this to be synced up with bar, but it's fine.

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