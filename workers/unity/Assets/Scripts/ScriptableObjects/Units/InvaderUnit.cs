using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaponSchema = MdgSchema.Common.Weapon;
using UnitSchema = MdgSchema.Units;

namespace MDG.ScriptableObjects.Units
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.UnitPath + "/InvaderUnit")]
    public class InvaderUnit : ScriptableObject
    {
        public WeaponSchema.WeaponType weaponType;
        public UnitSchema.UnitTypes UnitType;
        public float AttackRange;
    }
}