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

        Vector3 crosshairOffset = new Vector3(Screen.width * 0.8f, 0, 0);
        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnRequestSystem>();
            RectTransform rectTransform = GameObject.Find("DefenderHud").GetComponent<RectTransform>();
            crosshairOffset = new Vector3(rectTransform.rect.width * 0.3f, rectTransform.rect.height * 0.6f, 0);
            //GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };
            Weapon = Resources.Load("ScriptableObjects/Weapons/DefenderProjectile") as Weapon;
        }

        public void Shoot()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            crosshairs.transform.position = Input.mousePosition;
            Vector2 pos = crosshairs.GetChild(0).transform.position;
            Ray ray = shootCamera.ScreenPointToRay(pos);
            Physics.Raycast(ray , out RaycastHit raycastHit, Mathf.Infinity);
            Vector3 direction = raycastHit.point - shootOrigin.position;
            Debug.Log("raycast point" + raycastHit.point);
            Debug.Log("direction of shot " + direction);
            shootOrigin.rotation = Quaternion.LookRotation(direction);
            Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(shootOrigin.position);
            Projectile projectile = Weapon as Projectile;
            Vector3f bulletLinearVelocity = HelperFunctions.Scale(HelperFunctions.Vector3fFromUnityVector(shootOrigin.forward), projectile.ProjectileSpeed);

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