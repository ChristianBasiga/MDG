using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.Interfaces
{
    public interface IPlayerSynchronizer
    {
        void LinkClientWorker(UnityClientConnector unityClientConnector);
    }
}