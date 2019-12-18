using MDG.Common.MonoBehaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.Interfaces
{
    //[RequireComponent(typeof(InputProcessorManager))]
    public interface IProcessInput
    {
        void AddToManager();
        void ProcessInput();
    }
}