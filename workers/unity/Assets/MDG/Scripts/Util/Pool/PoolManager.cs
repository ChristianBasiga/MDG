using MDG.ScriptableObjects.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Game.Util.Pool
{
    public class PoolManager : MonoBehaviour
    {
        // Pool General GameObjects under type. More specific types will have additional componetns added through a builder.
        // Due to deadlines, do this later on.
        Dictionary<string, Queue<GameObject>> pools;
        const string PrefabPath = "Prefabs/UnityClient";

        // Inject this via zenject later.
        string configSetting = "BasePoolConfig";

        PoolConfig poolConfig;
        ResourceRequest poolConfigPromise;

        public void Start()
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
        private void Init(PoolConfig poolConfig)
        {
            pools = new Dictionary<string, Queue<GameObject>>();
            for (int i = 0; i < poolConfig.PoolConfigItems.Length; ++i)
            {
                PoolConfigItem poolConfigItem = poolConfig.PoolConfigItems[i];
                Debug.Log($"Creating pool for {poolConfigItem.prefabPath}");
                GameObject prefab = Resources.Load($"{PrefabPath}/{poolConfigItem.prefabPath}") as GameObject;
                StartCoroutine(LoadPool(prefab, poolConfigItem));
            }
        }


        IEnumerator LoadPool(GameObject prefab, PoolConfigItem poolConfigItem)
        {
            Queue<GameObject> pool = new Queue<GameObject>();
            string fullPath = $"{PrefabPath}/{poolConfigItem.prefabPath}";
            for (int i = 0; i < poolConfigItem.PoolSize; ++i)
            {
                GameObject instance = Instantiate(prefab);
                Reusable reusableComponent = instance.AddComponent<Reusable>();
                instance.SetActive(false);
                reusableComponent.OnReuse += (GameObject toReuse) =>
                {
                    pools[fullPath].Enqueue(toReuse);
                };
                pool.Enqueue(instance);
                if (i % 5 == 0)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            pools.Add(fullPath, pool);
        }

        public bool TryGetFromPool(string key, out GameObject gameObject)
        {
            if (pools.TryGetValue(key, out Queue<GameObject> queue))
            {
                gameObject = queue.Dequeue();
            }
            else
            {
                gameObject = null;
            }
            return gameObject != null;
        }
    }
}