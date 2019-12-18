﻿using Improbable;
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
using MdgSchema.Common.Util;

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

                // If can't physically place, this point spent needs to be revoked.
                // Add that later / restructure this.
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
            Vector3 worldCoords = HelperFunctions.GetMousePosition(playerCamera);

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity) && !hit.collider.CompareTag("Player"))
            {
                Vector3f trapPosition = HelperFunctions.Vector3fFromUnityVector(hit.point);
                trapPosition.Y = 10;
                spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
                {
                    Position = trapPosition,
                    TypeToSpawn = MdgSchema.Common.GameEntityTypes.Structure,
                }, OnTrapSpawned, Converters.SerializeArguments(trapConfig));
            }
        }

        void OnTrapSpawned(EntityId entityId)
        {
            Debug.Log($"Spawned trap with entity id = {entityId}");
        }
    }
}