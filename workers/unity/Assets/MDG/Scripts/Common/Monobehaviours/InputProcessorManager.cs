using MDG.Common.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Common.MonoBehaviours
{
    public class InputProcessorManager : MonoBehaviour
    {
        // Later on will be keyed based on game state.
        List<IProcessInput> inputProcessors;

        public void AddInputProcessor(IProcessInput inputProcessor)
        {
            if (inputProcessors == null)
            {
                inputProcessors = new List<IProcessInput>();
            }
            inputProcessors.Add(inputProcessor);
        }

        void Update()
        {

            if (Application.isPlaying && inputProcessors != null)
            {
                for (int i = 0; i < inputProcessors.Count; ++i)
                {
                    inputProcessors[i].ProcessInput();
                }
            }
        }
    }
}