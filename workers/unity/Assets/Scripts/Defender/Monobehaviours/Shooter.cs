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

public class Shooter : MonoBehaviour
{
    SpawnRequestSystem spawnRequestSystem;

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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnBullet();
        }
    }

    void SpawnBullet()
    {
        LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
        // rest should be derived from scriptble object shooter has. For now setting here.
        // Ideally, show projectile coming from weapon, then continuing on from crosshairs forward. That's polish end result is cross hair forward and that
        // is enugh.
        Vector3f bulletStartingPosition = HelperFunctions.Vector3fFromUnityVector(crossHairs.position);
        // Problem is that the sense of depth off.
        Vector3f bulletLinearVelocity = HelperFunctions.Vector3fFromUnityVector(crossHairs.forward * bulletSpeed);

        // Down line derive this info from currently equipped weapon by referencing scriptable object.
        ProjectileConfig projectileConfig = new ProjectileConfig
        {
            startingPosition = bulletStartingPosition,
            linearVelocity = bulletLinearVelocity,
            maximumHits = 1,
            damage = 1,
            projectileId = 1,
            dimensions = new Vector3f(5,0,5),
            lifeTime = 20.0f
        };
        byte[] serializedWeapondata = Converters.SerializeArguments(projectileConfig);
        WeaponMetadata weaponMetadata = new WeaponMetadata
        {
            weaponType = MdgSchema.Common.Weapon.WeaponType.Projectile,
            wielderId = linkedEntityComponent.EntityId.Id
        };
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
