using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.Systems.Spawn;
using MDG.DTO;
using MDG.ScriptableObjects.Items;
using MDG.ScriptableObjects.Units;
using MdgSchema.Common.Util;
using MdgSchema.Units;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;

// Need to change where this goes since longer monobehaviour.
namespace MDG.Invader.Monobehaviours.Structures
{
    // Will reudce methods in Istructure interface to only required as cna simply subscribe to events as needed.
    public class UnitSpawnerStructure : MonoBehaviour, IStructure
    {

        const string StoredUnitInfoPath = "ScriptableObjects/Units";
#pragma warning disable 649
        [Require] StructureSchema.StructureCommandSender structureCommandSender;
#pragma warning restore 649
        LinkedEntityComponent linkedStructure;
        SpawnRequestSystem spawnRequestSystem;
        Vector3f[] spawnPoints;
        Vector3f[] SpawnPoints {
            get
            {
                if (spawnPoints == null)
                {
                    Transform spawnArea = transform.Find("SpawnAreas");
                    spawnPoints = spawnArea.GetComponentsInChildren<Transform>().Skip(1).Select(t =>
                    {
                        Debug.Log("got spwn point " + t.position + "from " + t.name);
                        return new Vector3f(t.position.x, 20, t.position.z);
                    }).ToArray();
                }
                return spawnPoints;
            }
        }
        int spawnPointIndex;
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
                ResourceRequest promise = Resources.LoadAsync($"{StoredUnitInfoPath}/{name}Unit");
                promise.completed += (_) =>
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
            structureBehaviour.OnJobStarted += OnJobStarted;
        }

        private void OnJobStarted(int jobIndex, ShopItem shopItem, LinkedEntityComponent arg3)
        {
            spawnPointIndex = jobIndex;
        }

        // Create abstract class with startjob base sending StartJobRequest.
        // Should be in structure interface called start job as common argumens.
        public void StartJob(byte[] jobContext)
        {
            PurchasePayload purchasePayload = Converters.DeserializeArguments<PurchasePayload>(jobContext);
            ShopUnitDto shopUnit = purchasePayload.ShopItem as ShopUnitDto;

            // Should set combat stats too, etc. So from shop unit to load specific invader units.
            // So send run job requests with spawn request as serialzied payload.
            UnitConfig unitConfig = new UnitConfig
            {
                OwnerId = purchasePayload.PurchaserId,
                Position = SpawnPoints[spawnPointIndex],
                UnitType = shopUnit.UnitType,
            };
            Debug.Log("spawning at " + HelperFunctions.Vector3fToVector3(spawnPoints[spawnPointIndex]));

            Debug.Log("Shop unit construction time " + shopUnit.ConstructionTime);

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
                    EstimatedJobCompletion = shopUnit.ConstructionTime,
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
            unitConfig.Position.Y = 20;

            // Need to delay this to be synced up with bar, but it's fine.

            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Count = 1,
                Position = unitConfig.Position
            }, OnUnitSuccessfullySpawned, jobData);
        }


        private void OnUnitSuccessfullySpawned(EntityId entityId)
        {

        }

      
    }
}