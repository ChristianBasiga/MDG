using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;

namespace MDG.Lobby
{
    public class Templates
    {
        public static EntityTemplate CreateLobbyTemplate()
        {
            var template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot("Lobby"), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Position.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Persistence.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);
            return template;
        }
        //Need to rpob make another worker sot hat this one server worker isn't doing too much.
        //Also make a place to get all worker types I have so SST
        public static EntityTemplate CreateRoomTemplate()
        {
            var template = new EntityTemplate();

            System.Guid guid = System.Guid.NewGuid();
            //Fetch from pool of rooms later on. Each will be keyed by guid. But that's optimization.
            //Since rooms are whatever, I could just have simple ever increasing integer instead of GUID.
            template.AddComponent(new Room.Snapshot(0, new System.Collections.Generic.List<string>(), false, 0, false), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Metadata.Snapshot("Room"), UnityGameLogicConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);

            return template;
        }

    }
}