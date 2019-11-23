using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.DTO;
// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/BuildableItem")]
    public class BuildableItem : ShopItem
    {
        public int RequiredWorkersCount;
        public StructureConfig StructureConfig;
        public override bool Equals(object other)
        {
            BuildableItem otherItem = other as BuildableItem;
            return ItemId.Equals(otherItem.ItemId) && Title.Equals(otherItem.Title) && Cost.Equals(otherItem.Cost) &&
                RequiredWorkersCount.Equals(otherItem.RequiredWorkersCount);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}