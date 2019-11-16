using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/ShopItem")]
    public class ShopItem : InventoryItem
    {
        public Constants.ShopItemType shopItemType;
        public int Cost;
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