using Improbable.Gdk.Subscriptions;
using MDG.Common.Systems.Spawn;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;
using MDG.DTO;
using Improbable;
using MDG.Common;
using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Weapons;

namespace MDG.Common.MonoBehaviours
{
    public class Shooter : MonoBehaviour
    {
        SpawnRequestSystem spawnRequestSystem;

        // Need to figure out how this is set. I've got a trash project right now dude.
        Weapon weapon;

        [SerializeField]
        Transform crossHairs;
        [SerializeField]
        Transform shootOrigin;
        [SerializeField]
        float bulletSpeed = 50.0f;
        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();

        }

        public void SpawnBullet()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            // rest should be derived from scriptble object shooter has. For now setting here.
            // Ideally, show projectile coming from weapon, then continuing on from crosshairs forward. That's polish end result is cross hair forward and that
            // is enugh.
            Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(crossHairs.position);
            // Problem is that the sense of depth off.
            Vector3f bulletLinearVelocity = HelperFunctions.Vector3fFromUnityVector(crossHairs.forward * bulletSpeed);

            ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(weapon as Projectile);
            projectileConfig.startingPosition = bulletStartingPosition;
            projectileConfig.linearVelocity = bulletLinearVelocity;

            WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(weapon);
            weaponMetadata.wielderId = linkedEntityComponent.EntityId.Id;

            byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);
            byte[] serializedWeaponMetadata = Converters.SerializeArguments(weaponMetadata);
            spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                Position = bulletStartingPosition,
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Weapon,
                TypeId = 1,
                Count = 1
            }, OnBulletSpawned, serializedWeaponMetadata, serializedWeapondata);
        }

        void OnBulletSpawned(EntityId entityId)
        {
            // This would be for maybe adding  animation before hand, or querying remaining bullet
            // etc.
            Debug.Log("Spawned bullet");
        }
    }
}