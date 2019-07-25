using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using MdgSchema.Player;
using Improbable.Gdk.Subscriptions;
namespace MDG.Lobby
{
    // This will attach to UI prefab. Now to actually store the rooms.
    // 
    public class LobbyClientWorker : MonoBehaviour
    {
        [Require] LobbyCommandSender lobbyCommandSender;
        [Require] LobbyReader lobbyReader;

        //This will be set to null after updating. This makes sure that event updates after response doesn't reupdate the UI.
        bool updatingUI;
        string lastUpdateId;
        private void Start()
        {
            lobbyReader.OnJoinedRoomEvent += UpdateUI;
        }
        public void joinRoom(System.Int32 id, PlayerType playerType)
        {

            RoomJoinRequest payload = new RoomJoinRequest
            {
                Type = playerType,
                RoomJoining = id,
                UserName = "Test"
                
            };
            MdgSchema.Lobby.Lobby.JoinRoom.Request reqs = new MdgSchema.Lobby.Lobby.JoinRoom.Request
            {
                TargetEntityId = GetComponent<LinkedEntityComponent>().EntityId,
                Payload = payload
            };
            //If not specific, then command is broadcast, in this case only to one regardless of broadcast or specific.
            lobbyCommandSender.SendJoinRoomCommand(reqs,OnRoomJoined);
        }

        
        void OnRoomJoined(MdgSchema.Lobby.Lobby.JoinRoom.ReceivedResponse obj)
        {
            RoomJoinResponse payload = obj.ResponsePayload.Value;
            UpdateUI(payload);
        }


        void UpdateUI(RoomJoinResponse res)
        {
            // Makes sure if triggered by response, isn't triggered again by event. But if finishes updateing UI by time event triggers
            // then will do again, so this is soft prevention. Hard prevention would be adding id to response and seeing if they are the same.
            if (updatingUI || lastUpdateId == res.ResponseId)
            {
                //If gets by soft check, do hard check
                return;
            }

            updatingUI = true;
            //Updates UI.
            updatingUI = false;
            lastUpdateId = res.ResponseId;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                joinRoom(1, PlayerType.HUNTER);
            }
        }
    }
}