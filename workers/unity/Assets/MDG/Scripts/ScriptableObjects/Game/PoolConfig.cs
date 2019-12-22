using MdgSchema.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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