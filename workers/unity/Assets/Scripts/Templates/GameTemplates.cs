using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;

namespace MDG.Game
{
    public class Templates
    {
        public static EntityTemplate CreateGameManagerTemplate()
        {
            var template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot("GameManager"), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Position.Snapshot(), UnityGameLogicConnector.WorkerType); 
            template.AddComponent(new Persistence.Snapshot(), UnityGameLogicConnector.WorkerType);

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);

            return template;
        }
       

    }
}