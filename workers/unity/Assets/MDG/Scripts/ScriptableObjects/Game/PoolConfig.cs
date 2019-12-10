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
        public GameEntityTypes GameEntityType;
        public int PoolSize;
    }

    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/PoolConfig")]
    public class PoolConfig : ScriptableObject
    {
        public PoolConfigItem[] PoolConfigItems = System.Enum.GetValues(typeof(GameEntityTypes)).OfType<GameEntityTypes>().Select(type =>
        {
            return new PoolConfigItem
            {
                GameEntityType = type,
                PoolSize = 0
            };
        }).ToArray();
    }
}