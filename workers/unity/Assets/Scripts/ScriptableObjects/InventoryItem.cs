using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects
{
    [CreateAssetMenu]
    public class InventoryItem : ScriptableObject
    {
        public Mesh Mesh;
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