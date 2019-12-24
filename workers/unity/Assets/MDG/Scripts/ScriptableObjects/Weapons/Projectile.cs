using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Weapons
{
    // Config and scriptable objects will be good.
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.WeaponPath + "/Projectile")]
    public class Projectile : Weapon
    {
        public float LifeTime;
        public float ProjectileSpeed;

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