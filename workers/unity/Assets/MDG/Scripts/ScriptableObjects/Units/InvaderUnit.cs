using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponSchema = MdgSchema.Common.Weapon;
using UnitSchema = MdgSchema.Units;
using MDG.ScriptableObjects.Items;

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