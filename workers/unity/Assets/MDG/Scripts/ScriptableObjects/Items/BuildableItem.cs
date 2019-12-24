using MDG.DTO;
using UnityEngine;
// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Items
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.ItemPath + "/BuildableItem")]
    public class BuildableItem : ShopItem
    {
        public int RequiredWorkersCount;
        public StructureConfig StructureConfig;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}