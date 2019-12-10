

using Improbable;
using Improbable.Gdk.Core;
using MdgSchema.Common.Util;
using System;
using WeaponSchema = MdgSchema.Common.Weapon;
namespace MDG.DTO
{
    [Serializable]
    public class WeaponMetadata
    {
        public WeaponSchema.WeaponType weaponType;
        public string prefabName;
        public long wielderId;
        public float attackCooldown;

    }

    [Serializable]
    public class WeaponConfig
    {
        public int damage;
        public int maximumHits;
        public Vector3f dimensions;
    }

    [Serializable]
    public class ProjectileConfig: WeaponConfig
    {
        public float lifeTime;
        public Vector3f linearVelocity;
        public Vector3f angularVelocity;
        public Vector3f startingPosition;
        public int projectileId;
        public float projectileSpeed;
    }

    [Serializable]
    public class MeleeConfig : WeaponConfig
    {
        public float range;
    }
}