using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;
using GameSchema = MdgSchema.Game;
namespace MDG.Defender.Monobehaviours
{
    // Finish synchronization of defender.
    public class DefenderSynchronizer : MonoBehaviour
    {
        [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader;
        ComponentUpdateSystem componentUpdateSystem;

        public delegate void OnGameEndEventHandler();

        // Then UI, clean up, etc will attach to these events and act accordingly.
        // Prob move all this to GameSynchronizer Monobehaviour
        public OnGameEndEventHandler OnWinGame;
        public OnGameEndEventHandler OnLoseGame;
        public OnGameEndEventHandler OnEndGame;
        EntityId gameManagerEntityId;

        private void Start()
        {
            pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
            componentUpdateSystem = GetComponent<LinkedEntityComponent>().World.GetExistingSystem<ComponentUpdateSystem>();
            // Soo how should bullets be handled in ecs? I'm pretty sure they HAVE to be tempaltes.
            gameManagerEntityId = GameObject.FindGameObjectWithTag("GameManager").GetComponent<LinkedEntityComponent>().EntityId;
        }

        private void Update()
        {
            // For now just repeat this, later today move this to a common component
            var endGameEventMessages = componentUpdateSystem.GetEventsReceived<GameSchema.GameStatus.EndGame.Event>(gameManagerEntityId);
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
            // Trigger other things.
            gameObject.SetActive(!respawning);
        }
    }
}