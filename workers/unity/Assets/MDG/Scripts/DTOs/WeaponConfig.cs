using MdgSchema.Common.Util;
using System;
using WeaponSchema = MdgSchema.Common.Weapon;
namespace MDG.DTO
{
    [Serializable]
    public class WeaponMetadata
    {
        public WeaponSchema.WeaponType WeaponType;
        public string PrefabName;
        public long WielderId;
        public float AttackCooldown;
    }

    [Serializable]
    public class WeaponConfig
    {
        public int Damage;
        public int MaximumHits;
        public Vector3f Dimensions;
    }

    [Serializable]
    public class ProjectileConfig: WeaponConfig
    {
        public float LifeTime;
        public Vector3f LinearVelocity;
        public Vector3f AngularVelocity;
        public Vector3f StartingPosition;
        public int ProjectileId;
        public float ProjectileSpeed;
    }

    [Serializable]
    public class MeleeConfig : WeaponConfig
    {
        public float range;
    }
}