using UnityEngine;
using WeaponSchema = MdgSchema.Common.Weapon;
namespace MDG.ScriptableObjects.Units
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.UnitPath + "/DefenderUnit")]
    public class DefenderUnit : ScriptableObject
    {
        public WeaponSchema.WeaponType weaponType;
        public float MovementRadius;
        public float AttackRange;
    }
}