using System.Collections;
using System.Collections.Generic;

namespace MDG.ScriptableObjects
{
    public static class Constants 
    {
        public const string RootMenuPath = "MDG";
        public const string WeaponPath = "Weapons";
        public const string UnitPath = "Units";
        public const string ItemPath = "Items";
        public const string StructurePath = "Structures";
        public enum ShopItemType
        {
            Buildable,
            Unit,
            Weapon
        }
    }
}