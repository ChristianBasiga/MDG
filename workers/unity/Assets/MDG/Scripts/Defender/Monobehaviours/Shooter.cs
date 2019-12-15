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
using MdgSchema.Common.Util;

namespace MDG.Defender.Monobehaviours
{
    public class Shooter : MonoBehaviour
    {
        SpawnRequestSystem spawnRequestSystem;

        public Weapon Weapon { private set; get; }

#pragma warning disable 649

        [SerializeField]
        Camera shootCamera;

        [SerializeField]
        Transform shootOrigin;

        [SerializeField]
        Transform crosshairs;

#pragma warning restore 649

        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            Weapon = Resources.Load("ScriptableObjects/Weapons/DefenderProjectile") as Weapon;
        }

        public void Shoot()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            crosshairs.transform.position = Input.mousePosition;
            Vector2 pos = crosshairs.GetChild(0).transform.position;
            Ray ray = shootCamera.ScreenPointToRay(pos);
            shootOrigin.rotation = Quaternion.LookRotation(ray.direction);

            Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(shootOrigin.position);
            Projectile projectile = Weapon as Projectile;
            Vector3f bulletLinearVelocity = HelperFunctions.Scale(HelperFunctions.Vector3fFromUnityVector(ray.direction), projectile.ProjectileSpeed);

            ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(projectile);
            projectileConfig.startingPosition = bulletStartingPosition;
            projectileConfig.linearVelocity = bulletLinearVelocity;

            WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(Weapon);
            weaponMetadata.wielderId = linkedEntityComponent.EntityId.Id;

            byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);
            byte[] serializedWeaponMetadata = Converters.SerializeArguments(weaponMetadata);

            spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                Position = bulletStartingPosition,
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Weapon,
            }, OnBulletSpawned, serializedWeaponMetadata, serializedWeapondata);
        }

        void OnBulletSpawned(EntityId entityId)
        {
        }
    }
}