using MDG.ScriptableObjects.Items;
using UnityEngine;
using UnitSchema = MdgSchema.Units;
using WeaponSchema = MdgSchema.Common.Weapon;

namespace MDG.ScriptableObjects.Units
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.UnitPath + "/InvaderUnit")]
    public class InvaderUnit : ShopItem
    {
        public WeaponSchema.WeaponType WeaponType;
        public UnitSchema.UnitTypes UnitType;
        public float AttackRange;
        public float AttackCooldown;
    }
}