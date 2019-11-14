using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects
{
    [CreateAssetMenu]
    public class BuildableItem : ShopItem
    {
        public int RequiredWorkers;
        public override bool Equals(object other)
        {
            BuildableItem otherItem = other as BuildableItem;
            return ItemId.Equals(otherItem.ItemId) && Title.Equals(otherItem.Title) && Cost.Equals(otherItem.Cost) &&
                RequiredWorkers.Equals(otherItem.RequiredWorkers);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}