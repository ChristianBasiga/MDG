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
        public delegate void OnGameEndEventHandler();
        public OnGameEndEventHandler OnWinGame;
        public OnGameEndEventHandler OnLoseGame;
        public OnGameEndEventHandler OnEndGame;

        private void Start()
        {
            clientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
            componentUpdateSystem = GetComponent<LinkedEntityComponent>().World.GetExistingSystem<ComponentUpdateSystem>();
            GetComponent<HealthSynchronizer>().OnHealthBarUpdated += OnHealthUpdate;
        }


        private void OnHealthUpdate(int currentHealth)
        {
            if (currentHealth == 0)
            {
                this.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // For now just repeat this, later today move this to a common component
            var endGameEventMessages = componentUpdateSystem.GetEventsReceived<GameSchema.GameStatus.EndGame.Event>(clientConnector.GameManagerEntity.SpatialOSEntityId);
            if (endGameEventMessages.Count > 0)
            {
                ref readonly var endGameEvent = ref endGameEventMessages[0];

                switch (endGameEvent.Event.Payload.WinConditionMet)
                {
                    case GameSchema.WinConditions.TimedOut:
                        OnWinGame?.Invoke();
                        break;
                    case GameSchema.WinConditions.TerritoriesClaimed:
                        OnLoseGame?.Invoke();
                        break;
                }
                OnEndGame?.Invoke();
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