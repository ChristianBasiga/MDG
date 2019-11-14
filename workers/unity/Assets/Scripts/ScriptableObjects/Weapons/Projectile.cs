using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponSchema = MdgSchema.Common.Weapon;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects
{
    // Config and scriptable objects will be good.
    [CreateAssetMenu]
    public class Projectile : Weapon
    {
        public float LifeTime;

        public override bool Equals(object other)
        {
            Weapon otherItem = other as Weapon;
            return WeaponId.Equals(otherItem.WeaponId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}