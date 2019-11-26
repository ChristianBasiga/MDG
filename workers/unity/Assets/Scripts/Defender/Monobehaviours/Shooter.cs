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

namespace MDG.Defender.Monobehaviours
{
    public class Shooter : MonoBehaviour
    {
        SpawnRequestSystem spawnRequestSystem;

        Weapon weapon;
        Transform crossHairs;
        Transform shootOrigin;

        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            crossHairs = GameObject.Find("CrossHairs").transform;
            shootOrigin = GameObject.Find("ShootOrigin").transform;
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };
            weapon = Resources.Load("ScriptableObjects/Weapons/DefenderProjectile") as Weapon;
        }

        public void SpawnBullet()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            // rest should be derived from scriptble object shooter has. For now setting here.
            // Ideally, show projectile coming from weapon, then continuing on from crosshairs forward. That's polish end result is cross hair forward and that
            // is enugh.
            Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(crossHairs.position);

            Projectile projectile = weapon as Projectile;
            Vector3f bulletLinearVelocity = HelperFunctions.Vector3fFromUnityVector(crossHairs.forward) * projectile.ProjectileSpeed;

            ProjectileConfig projectileConfig = Converters.ProjectileToProjectileConfig(projectile);
            projectileConfig.startingPosition = bulletStartingPosition;
            projectileConfig.linearVelocity = bulletLinearVelocity;

            WeaponMetadata weaponMetadata = Converters.WeaponToWeaponMetadata(weapon);
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