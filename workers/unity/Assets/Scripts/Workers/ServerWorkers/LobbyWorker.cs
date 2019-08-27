using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MdgSchema.Lobby;
using Improbable.Gdk.Subscriptions;

namespace MDG.Lobby
{

    //For tetsing just put here.
    public class Room
    {
        public bool hunterJoined = false;
        //List of players;
        public int playerCount = 0;
        const int maxRoomSize = 4;
        public bool IsRoomFilled()
        {
            return playerCount == maxRoomSize;
        }
    }
    // This worker manages writing of rooms.
    // It will intercept joinRoom commands made to room components
    // and write accordingly.
    public class LobbyWorker : MonoBehaviour
    {
        // Add command to init the rooms
        [Require] LobbyCommandReceiver lobbyCommandReceiver;
        [Require] LobbyWriter lobbyWriter;
        List<Room> rooms;
        // Need to reference pool data and max count, for now just magic number.
        const int MaxRooms = 5;
        private void Start()
        {
            lobbyCommandReceiver.OnJoinRoomRequestReceived += OnJoinRoomRequestReceived;
            rooms = new List<Room>();
            //Populate rooms, then send update for clients to make activate same amount of rooms from pool.
            for (int i = 0; i < MaxRooms; ++i)
            {
                rooms.Add(new Room());
            }
        }
    
        private void OnJoinRoomRequestReceived(MdgSchema.Lobby.Lobby.JoinRoom.ReceivedRequest req)
        {
            //List<Int32> roomKeys = lobbyWriter.Data.Rooms;
            RoomJoinResponse payload = JoinRoom(req.Payload);
            MdgSchema.Lobby.Lobby.JoinRoom.Response response = new MdgSchema.Lobby.Lobby.JoinRoom.Response
            {
                Payload = payload,
                RequestId = req.RequestId
                
            };
            lobbyCommandReceiver.SendJoinRoomResponse(response);
            if (payload.Joined)
            {
                lobbyWriter.SendJoinedRoomEvent(payload);
            }
        }

        private RoomJoinResponse JoinRoom(MdgSchema.Lobby.RoomJoinRequest payload)
        {
            //Honestly roomId is extra
            Debug.LogError("Room id " + payload.RoomId);

            bool isHunter = payload.Type == MdgSchema.Player.PlayerType.HUNTER;
            Room room = rooms[payload.RoomId];
            if (room.IsRoomFilled() || (room.hunterJoined && isHunter))
            {
                return new RoomJoinResponse { Joined = false };
            }
            room.hunterJoined = isHunter; 
            room.playerCount += 1;
            System.Guid guid = System.Guid.NewGuid();
            return new RoomJoinResponse {
                Joined = true,
                HunterJoined = room.hunterJoined,
                RoomSize = room.playerCount,
                ResponseId = guid.ToString(),
                RoomId = payload.RoomId
               
            };
       }

    }
}