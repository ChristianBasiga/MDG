using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;
using GameSchema = MdgSchema.Game;
using MDG.ScriptableObjects.Game;
using MDG.Common.MonoBehaviours;

namespace MDG.Defender.Monobehaviours
{
    // Finish synchronization of defender.
    // Change this to PlayerSynchronizer as common end game behaviour
    public class DefenderSynchronizer : MonoBehaviour
    {
#pragma warning disable 649

        [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader;


        ComponentUpdateSystem componentUpdateSystem;
        [SerializeField]
        public Camera Camera { set; get; }
#pragma warning restore 649


        UnityClientConnector clientConnector;

        private void Start()
        {
            clientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
            componentUpdateSystem = GetComponent<LinkedEntityComponent>().World.GetExistingSystem<ComponentUpdateSystem>();
            GetComponent<HealthSynchronizer>().OnHealthBarUpdated += OnHealthUpdate;
        }

        private void GameStatusSynchronizer_OnWinGame()
        {
            throw new System.NotImplementedException();
        }

        private void OnHealthUpdate(int percentageHealth)
        {
            if (percentageHealth == 1)
            {
                gameObject.SetActive(false);
            }
        }


        private void OnRespawnActiveChange(bool respawning)
        {
            if (!respawning)
            {
                gameObject.SetActive(true);
            }
        }
    }
}