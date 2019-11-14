using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/InventoryItem")]
    public class InventoryItem : ScriptableObject
    {
        public Sprite ArtWork;
        public int ItemId;
        public string Title;
        public override bool Equals(object other)
        {
            InventoryItem otherItem = other as InventoryItem;
            return ItemId.Equals(otherItem.ItemId) && Title.Equals(otherItem.Title);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}