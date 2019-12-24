using UnityEngine;
using UnitSchema = MdgSchema.Units;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/ShopUnit")]
    public class ShopUnit : ShopItem
    {
        public float ConstructTime;
        public UnitSchema.UnitTypes UnitType;
    }
}