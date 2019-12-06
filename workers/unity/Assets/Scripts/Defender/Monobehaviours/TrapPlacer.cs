using Improbable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScriptableStructures = MDG.ScriptableObjects.Structures;
using MDG.Common.Systems.Spawn;
using Improbable.Gdk.Subscriptions;
using MDG.DTO;
using MDG.Common;
using Improbable.Gdk.Core;
using MDG.Common.Systems.Point;
using MdgSchema.Common.Point;

namespace MDG.Defender.Monobehaviours
{
    public class TrapPlacer : MonoBehaviour
    {
        LinkedEntityComponent linkedEntityComponent;

        [SerializeField]
        GameObject SelectionGrid;

        [Require] PointReader pointReader = null;


        PointRequest? pointRequestSent = null;

        // Start is called before the first frame update
        void Start()
        {
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void TryPlaceTrap(ScriptableStructures.Trap trap)
        {
            int comparingValue = pointRequestSent.HasValue ? pointReader.Data.Value - pointRequestSent.Value.PointUpdate : pointReader.Data.Value;
            if (comparingValue < trap.Cost)
            {
                // Should be thrown as erro then caught by hud instead
                GetComponent<DefenderHUD>().SetErrorText("Not enough points");
            }
            else
            {
                PointRequestSystem pointRequestSystem = linkedEntityComponent.World.GetExistingSystem<PointRequestSystem>();
                pointRequestSystem.AddPointRequest(new PointRequest
                {
                    EntityUpdating = linkedEntityComponent.EntityId,
                    PointUpdate = -trap.Cost
                });
                PlaceTrap(trap);
            }
        }


        // Perhaps let placing trap be a monobehaviour on it's own like shooter is.
        // wll be better for later setting up UI for where to place trap.
        public void PlaceTrap(ScriptableStructures.Trap trap)
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
            // How i get this position prod needs to change.
            Vector3 worldCoords = HelperFunctions.GetMousePosition(Camera.main);
            Vector3f trapPosition = new Vector3f(worldCoords.x, 10, worldCoords.z);
            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                Position = trapPosition,
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Structure,
            }, OnTrapSpawned, Converters.SerializeArguments(trapConfig));
        }

        void OnTrapSpawned(EntityId entityId)
        {
            Debug.Log($"Spawned trap with entity id = {entityId}");
        }
    }
}