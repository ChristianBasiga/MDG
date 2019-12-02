﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/InventoryItem")]
    public class InventoryItem : ScriptableObject
    {
        public Sprite Thumbnail;
        public string PrefabPath;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}