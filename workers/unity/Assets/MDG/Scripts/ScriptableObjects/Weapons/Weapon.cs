using UnityEngine;
using WeaponSchema = MdgSchema.Common.Weapon;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Weapons
{
    // Config and scriptable objects will be good.
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.WeaponPath + "/Weapon")]
    public class Weapon : ScriptableObject
    {
        public WeaponSchema.WeaponType weaponType;
        public Sprite ArtWork;
        public float AttackCooldown;
        public int WeaponId;
        public int Damage;
        public int Durability;
        public float Range;
        public string Title;
        public string PrefabPath;
        public Vector3 Dimensions;

        public override bool Equals(object other)
        {
            Weapon otherItem = other as Weapon;
            return WeaponId.Equals(otherItem.WeaponId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"AttackCooldown {AttackCooldown} Damage: {Damage} Durability: {Durability} Dimensions: {Dimensions}";
        }
    }
}