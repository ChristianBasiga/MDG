using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using MdgSchema.Player;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using System;
using Improbable;

namespace MDG.Lobby
{
    //This receives click info and sends command to LobbyServerWorker accordingly.
    //Updates UI of client.
    //Not conncector, probably will just be component add to room.

    public class LobbyClientWorker : MonoBehaviour
    {
        [Require] RoomCommandSender commandSender;
        void joinRoom(int id, PlayerType playerType)
        {
            RoomJoinRequest payload = new RoomJoinRequest
            {
                Type = playerType
            };
            Room.JoinRoom.Request reqs = new Room.JoinRoom.Request
            {
                TargetEntityId = GetComponent<LinkedEntityComponent>().EntityId,
                Payload = payload
            };
            //If not specific, then command is broadcast, in this case only to one regardless of broadcast or specific.
            commandSender.SendJoinRoomCommand(reqs,OnRoomJoined);
        }

        void OnRoomJoined(Room.JoinRoom.ReceivedResponse obj)
        {
                Debug.LogError("Respones " + obj.SendingEntity.Index);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                joinRoom(1, PlayerType.HUNTER);
            }
        }
    }
}