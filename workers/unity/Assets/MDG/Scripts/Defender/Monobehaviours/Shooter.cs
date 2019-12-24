using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.Systems.Spawn;
using MDG.DTO;
using MDG.ScriptableObjects.Weapons;
using MdgSchema.Common.Util;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;

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

#pragma warning restore 649


        float timeSinceLastShot = 0;

        

        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            Weapon = Resources.Load("ScriptableObjects/Weapons/DefenderProjectile") as Weapon;
        }

        public void Shoot()
        {
           /* if (timeSinceLastShot > 0)
            {
                timeSinceLastShot -= Time.deltaTime;
            }
            else
            {
                timeSinceLastShot = Weapon.AttackCooldown;*/
                LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();

                Ray ray = shootCamera.ScreenPointToRay(Input.mousePosition);
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
           // }
        }

        void OnBulletSpawned(EntityId entityId)
        {
        }
    }
}