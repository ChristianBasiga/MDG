using UnityEngine;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/ShopItem")]
    public class ShopItem : InventoryItem
    {
        public Constants.ShopItemType shopItemType;
        public int Cost;
    }
}