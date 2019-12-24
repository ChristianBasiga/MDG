using Improbable.Gdk.Core;
using MdgSchema.Common;
using System;
using UnityEngine;
using GameSchema = MdgSchema.Game;

namespace MDG.Common.MonoBehaviours.Synchronizers
{
    public class GameStatusSynchronizer : MonoBehaviour
    {
        public event Action<GameSchema.StartGameEventPayload> OnStartGame;
        public event Action<float> OnUpdateTime;
        public event Action<string> OnWinGame;
        public event Action<string> OnLoseGame;
        public event Action OnEndGame;
        ComponentUpdateSystem componentUpdateSystem;
        UnityClientConnector clientConnector;
        MainOverlayHUD mainOverlayHUD;
        // Start is called before the first frame update
        void Start()
        {
            clientConnector = GetComponent<UnityClientConnector>();
            mainOverlayHUD = GetComponent<MainOverlayHUD>();
            componentUpdateSystem = clientConnector.Worker.World.GetExistingSystem<ComponentUpdateSystem>();

        }

        // Update is called once per frame
        void Update()
        {
            var startGameEvent = componentUpdateSystem.GetEventsReceived<GameSchema.GameStatus.StartGame.Event>(clientConnector.GameManagerEntity.SpatialOSEntityId);
            if (startGameEvent.Count > 0)
            {
                OnStartGame?.Invoke(startGameEvent[0].Event.Payload);
            }

            var statusSnapshot = componentUpdateSystem.GetComponent<GameSchema.GameStatus.Snapshot>(clientConnector.GameManagerEntity.SpatialOSEntityId);
            OnUpdateTime?.Invoke(statusSnapshot.TimeLeft);
            // For now just repeat this, later today move this to a common component
            var endGameEventMessages = componentUpdateSystem.GetEventsReceived<GameSchema.GameStatus.EndGame.Event>
                (clientConnector.GameManagerEntity.SpatialOSEntityId);
            Debug.Log("end game event message count " + endGameEventMessages.Count);
            if (endGameEventMessages.Count > 0)
            {
                ref readonly var endGameEvent = ref endGameEventMessages[0];

                switch (endGameEvent.Event.Payload.WinConditionMet)
                {
                    case GameSchema.WinConditions.TimedOut:
                        switch (clientConnector.PlayerRole)
                        {
                            case GameEntityTypes.Hunted:
                                OnWinGame?.Invoke("You have defended");
                                break;
                            case GameEntityTypes.Hunter:
                                OnLoseGame?.Invoke("You have failed to invade");
                                break;
                        }
                        break;
                    case GameSchema.WinConditions.TerritoriesClaimed:
                        switch (clientConnector.PlayerRole)
                        {
                            case GameEntityTypes.Hunted:
                                OnLoseGame?.Invoke("You have failed to defend");
                                break;
                            case GameEntityTypes.Hunter:
                                OnWinGame?.Invoke("You have invaded");
                                break;
                        }
                        break;
                }
                OnEndGame?.Invoke();
            }
        }
    }
}