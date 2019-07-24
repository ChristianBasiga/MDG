using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MdgSchema.Lobby;
using Improbable.Gdk.Subscriptions;

namespace MDG.Lobby
{
    // This worker manages writing of rooms.
    // It will intercept joinRoom commands made to room components
    // and write accordingly.
    public class LobbyWorker : MonoBehaviour
    {
        [Require] RoomCommandReceiver roomJoinReceiver;
        bool addedReceiverCallback = false;


        private void Start()
        {
            roomJoinReceiver.OnJoinRoomRequestReceived += OnJoinRoomRequestReceived;
        }
        private void OnJoinRoomRequestReceived(Room.JoinRoom.ReceivedRequest obj)
        {
            Debug.Log("Received req");
            //Checks if can join room.
            RoomJoinResponse payload = new RoomJoinResponse
            {
                Joined = true
            };
            Room.JoinRoom.Response res = new Room.JoinRoom.Response
            {
                Payload = payload
            };
            roomJoinReceiver.SendJoinRoomResponse(res); 
        }
        // Update is called once per frame
        void Update()
        {
        }
    }
}