using MDG.ScriptableObjects.Game;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

namespace MDG.Game.Util.Pool
{
    public class PoolManager
    {
        // Pool General GameObjects under type. More specific types will have additional componetns added through a builder.
        // Due to deadlines, do this later on.
        Dictionary<GameEntityTypes, Queue<GameObject>> pools;
        const string PrefabPath = "Prefabs/UnityClient";

        // Inject this via zenject later.
        string configSetting = "Base";

        PoolConfig poolConfig;
        ResourceRequest poolConfigPromise;

        public PoolManager()
        {
            poolConfigPromise = Resources.LoadAsync<PoolConfig>($"ScriptableObjects/GameConfigs/PoolConfigs/{configSetting}");
            poolConfigPromise.completed += OnPoolConfigLoaded;
        }

        private void OnPoolConfigLoaded(AsyncOperation obj)
        {
            if (obj.isDone)
            {
                poolConfig = poolConfigPromise.asset as PoolConfig;
                Init(poolConfig);
            }
        }

        // Async due to the I/O calls.
        private async void Init(PoolConfig poolConfig)
        {
            pools = new Dictionary<GameEntityTypes, Queue<GameObject>>();
            var gameEntityTypes = System.Enum.GetValues(typeof(GameEntityTypes));
            foreach (GameEntityTypes gameEntityType in gameEntityTypes)
            {
                Queue<GameObject> pool = new Queue<GameObject>();
                string enumString = gameEntityType.ToString();
                GameObject prefab = Resources.Load($"{PrefabPath}/{enumString}") as GameObject;

                await Task.Run(() =>
                {
                    PoolConfigItem poolConfigItem = poolConfig.PoolConfigItems.First(x => x.GameEntityType.Equals(gameEntityType));
                    for (int i = 0; i < poolConfigItem.PoolSize; ++i)
                    {
                        GameObject instance = MonoBehaviour.Instantiate(prefab);
                        Reusable reusableComponent = instance.AddComponent<Reusable>();
                        reusableComponent.OnReuse += (GameObject toReuse) =>
                        {
                            pools[gameEntityType].Enqueue(toReuse);
                        };
                        instance.SetActive(false);
                        pool.Enqueue(instance);
                    }
                });
            }
        }


        public GameObject GetFromPool(GameEntityTypes gameEntityType)
        {
            GameObject gameObject = pools[gameEntityType].Dequeue();
            gameObject.SetActive(true);
            return gameObject;
        }
    }
}