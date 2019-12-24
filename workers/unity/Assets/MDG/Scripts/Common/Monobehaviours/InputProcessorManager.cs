using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours.Synchronizers;
using System.Collections.Generic;
using UnityEngine;
using GameSchema = MdgSchema.Game;
namespace MDG.Common.MonoBehaviours
{
    public class InputProcessorManager : MonoBehaviour
    {
        // Later on will be keyed based on game state.
        List<IProcessInput> inputProcessors;

        Dictionary<GameSchema.GameStates, List<IProcessInput>> inputHandlers;
        bool endedGame;
        public void AddInputProcessor(IProcessInput inputProcessor)
        {
            if (inputProcessors == null)
            {
                inputProcessors = new List<IProcessInput>();
            }
            inputProcessors.Add(inputProcessor);
        }

        // Should this be coupled to game stats synchronizer?
        // I mean state 
        public void SetSynchronizer(GameStatusSynchronizer gameStatusSynchronizer)
        {
            gameStatusSynchronizer.OnStartGame += EnableInputProcessors;
            gameStatusSynchronizer.OnEndGame += DisableInputProcessors;
        }

        public void EnableInputProcessors(MdgSchema.Game.StartGameEventPayload startGameEventPayload)
        {
            Debug.Log($"Starting game with session id {startGameEventPayload.SessionId}");
            for (int i = 0; i < inputProcessors.Count; ++i)
            {
                inputProcessors[i].Enable();
            }
        }


        public void DisableInputProcessors()
        {
            for (int i = 0; i < inputProcessors.Count; ++i)
            {
                inputProcessors[i].Disable();
            }
            endedGame = true;
        }

        void Update()
        {

            if (Application.isPlaying && inputProcessors != null && !endedGame)
            {
                Debug.Log("Still happening when game over");
                for (int i = 0; i < inputProcessors.Count; ++i)
                {
                    inputProcessors[i].ProcessInput();
                }
            }
        }
    }
}