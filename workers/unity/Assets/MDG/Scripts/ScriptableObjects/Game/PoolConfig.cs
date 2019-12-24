using System;
using UnityEngine;
namespace MDG.ScriptableObjects.Game
{
    [Serializable]
    public class PoolConfigItem
    {
        [SerializeField]
        public string prefabPath;
        public int PoolSize;
    }

    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/PoolConfig")]
    public class PoolConfig : ScriptableObject
    {
        public PoolConfigItem[] PoolConfigItems;
    }
}