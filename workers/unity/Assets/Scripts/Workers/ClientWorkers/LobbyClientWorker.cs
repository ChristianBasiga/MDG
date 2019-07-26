using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using MdgSchema.Player;
using Improbable.Gdk.Subscriptions;
using UnityEngine.UI;

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
        private const string pathToRoomPrefab = "Room";
        
        // This maps to collection of room objects in lobby, these rooms will get their UI updated accordingly
        private List<GameObject> rooms;
        string lastUpdateId;
        
        private void Start()
        {
            lobbyReader.OnJoinedRoomEvent += UpdateUI;
            Transform canvas = transform.GetChild(0);
            //Can figure out better designed way later, but just get this working, then based on this
            //can redesign it as well as start designing how the hunter will control units.
            //Get reference to buttons.
            rooms = new List<GameObject>();

            //Instead of iterating through canvas, will be going through pool.
            for (int i = 0; i < canvas.childCount; ++i)
            {
                GameObject room = canvas.GetChild(i).gameObject;
                Button joinButton = room.transform.Find("JoinButton").GetComponent<Button>();
                //For closure.
                int room_id = i;
                joinButton.onClick.AddListener( delegate()
                {
                   
                    //This normally fixes issue.
                    //Joined in will actually be derived from active player in scene, need to figure out how will store that better.
                    //Going ham for POC, but planning should be to design this well. But once working can always redesign for rest of sem.
                    this.JoinRoom(room_id, PlayerType.HUNTER);
                });
                rooms.Add(room);
            }

        }

        //Need player object or playerdto so send around for this stuff.
        public void JoinRoom(int id, PlayerType playerType)
        {
            RoomJoinRequest payload = new RoomJoinRequest
            {
                Type = playerType,
                RoomId = id,
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
            // Doesn't break anything if does, just needless computation.
            if (updatingUI || lastUpdateId == res.ResponseId)
            {
                //If gets by soft check, do hard check
                return;
            }
            updatingUI = true;
            //Updates UI of updated room.
            GameObject roomToUpdate = rooms[res.RoomId];
            Text roomFilledLabel = roomToUpdate.transform.GetChild(0).GetComponent<Text>();
            Text hunterJoinedLabel = roomToUpdate.transform.GetChild(1).GetComponent<Text>();
            Text huntedJoinedLabel = roomToUpdate.transform.GetChild(2).GetComponent<Text>();

            roomFilledLabel.text = res.RoomSize == 4? "Filled" : $"{res.RoomSize / 4}";
            hunterJoinedLabel.text = res.HunterJoined ? "Hunters 1/1" : "0/1";
            huntedJoinedLabel.text = $"{res.RoomSize - (res.HunterJoined ? 0 : 1)} / 4";

            updatingUI = false;
            lastUpdateId = res.ResponseId;
        }
    }
}