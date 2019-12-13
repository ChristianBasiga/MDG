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
            //GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };
            Weapon = Resources.Load("ScriptableObjects/Weapons/DefenderProjectile") as Weapon;
        }

        public void Shoot()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            crosshairs.transform.position = Input.mousePosition;
            Vector3 mousePos = HelperFunctions.GetMousePosition(shootCamera);

            Vector3 direction = mousePos - shootOrigin.position;
            shootOrigin.rotation = Quaternion.LookRotation(direction);
            Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(shootOrigin.position);
            Projectile projectile = Weapon as Projectile;
            Vector3f bulletLinearVelocity = HelperFunctions.Scale(HelperFunctions.Vector3fFromUnityVector(shootOrigin.forward), projectile.ProjectileSpeed);

            ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(projectile);
            projectileConfig.startingPosition = bulletStartingPosition;
            projectileConfig.linearVelocity = bulletLinearVelocity;

            WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(Weapon);
            weaponMetadata.wielderId = linkedEntityComponent.EntityId.Id;

            byte[] serializedWeapondata = Converters.SerializeArguments<ProjectileConfig>(projectileConfig);
            byte[] serializedWeaponMetadata = Converters.SerializeArguments<WeaponMetadata>(weaponMetadata);

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