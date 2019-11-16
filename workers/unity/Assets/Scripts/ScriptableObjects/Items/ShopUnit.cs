using System.Collections;
using System.Collections.Generic;
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
        public override bool Equals(object other)
        {
            ShopItem otherItem = other as ShopItem;
            return ItemId.Equals(otherItem.ItemId) && Title.Equals(otherItem.Title) && Cost.Equals(otherItem.Cost);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}