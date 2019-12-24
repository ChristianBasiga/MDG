using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.Systems.Point;
using MDG.Common.Systems.Spawn;
using MDG.DTO;
using MdgSchema.Common.Point;
using MdgSchema.Common.Util;
using System.Linq;
using UnityEngine;
using ScriptableStructures = MDG.ScriptableObjects.Structures;

namespace MDG.Defender.Monobehaviours
{
    public class TrapPlacer : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;

#pragma warning disable 649
        [SerializeField]
        GameObject SelectionGrid;

        [SerializeField]
        Camera playerCamera;
        [Require] PointReader pointReader;
#pragma warning restore 649


        DefenderSynchronizer defenderSynchronizer;
        PointRequest? pointRequestSent = null;

        // Start is called before the first frame update
        void Start()
        {
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();

            defenderSynchronizer = GetComponent<DefenderSynchronizer>();
        }

        public void TryPlaceTrap(ScriptableStructures.Trap trap)
        {
            int comparingValue = pointRequestSent.HasValue ? pointReader.Data.Value - pointRequestSent.Value.PointUpdate : pointReader.Data.Value;
            if (comparingValue < trap.Cost)
            {
                // Should be thrown as erro then caught by hud instead
                defenderSynchronizer.DefenderHUD.SetErrorText("Not enough points");
            }
            else
            {
                PointRequestSystem pointRequestSystem = linkedEntityComponent.World.GetExistingSystem<PointRequestSystem>();

                if (PlaceTrap(trap))
                {
                    pointRequestSystem.AddPointRequest(new PointRequest
                    {
                        EntityUpdating = linkedEntityComponent.EntityId,
                        PointUpdate = -trap.Cost
                    });
                }
            }
        }


        // Perhaps let placing trap be a monobehaviour on it's own like shooter is.
        // wll be better for later setting up UI for where to place trap.
        public bool PlaceTrap(ScriptableStructures.Trap trap)
        {
            SpawnRequestSystem spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            TrapConfig trapConfig = new TrapConfig
            {
                prefabName = trap.PrefabPath,
                Damage = trap.Damage,
                structureType = MdgSchema.Common.Structure.StructureType.Trap,
                ColliderDimensions = HelperFunctions.Vector3fFromUnityVector(trap.ColliderDimensions),
                constructionTime = trap.SetupTime,
                OneTimeUse = trap.OneTimeUse,
                ownerId = linkedEntityComponent.EntityId.Id
            };
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] raycastHits = Physics.RaycastAll(ray, Mathf.Infinity);
            RaycastHit? hit = raycastHits.FirstOrDefault((potential) =>
            {
                return !potential.collider.CompareTag("Player");
            });
            if (hit.HasValue)
            {
                Vector3f trapPosition = HelperFunctions.Vector3fFromUnityVector(hit.Value.point);
                trapPosition.Y = 10;
                spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
                {
                    Position = trapPosition,
                    TypeToSpawn = MdgSchema.Common.GameEntityTypes.Structure,
                }, OnTrapSpawned, Converters.SerializeArguments(trapConfig));
                return true;
            }
            else
            {
                Debug.Log("Ever here?");
                // Else return the cost to points as chose incorrect stop.
                return false;
            }
        }

        void OnTrapSpawned(EntityId entityId)
        {
            Debug.Log($"Spawned trap with entity id = {entityId}");
        }
    }
}