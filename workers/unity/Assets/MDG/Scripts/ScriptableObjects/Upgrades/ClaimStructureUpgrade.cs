using UnityEngine;


namespace MDG.ScriptableObjects.Upgrades
{
    [CreateAssetMenu(menuName = Constants.UpgradePath + "/ClaimStructureUpgrade")]
    public class ClaimStructureUpgrade : ScriptableObject
    {
        public Sprite Thumbnail;
        public string UpgradeName;
        public string UpgradeDescription;
    }
}