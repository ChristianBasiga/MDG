using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MdgSchema.Lobby;
using Improbable.Gdk.Subscriptions;

namespace MDG.Lobby
{

    //For tetsing just put here.
    public struct Room
    {
        public bool hunterJoined;
        //List of players;
        public int playerCount;
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
        [Require] LobbyCommandReceiver lobbyCommandReceiver;
        [Require] LobbyWriter lobbyWriter;
        List<Room> rooms;

        private void Start()
        {
            lobbyCommandReceiver.OnJoinRoomRequestReceived += OnJoinRoomRequestReceived;
        }
   
        private void OnJoinRoomRequestReceived(MdgSchema.Lobby.Lobby.JoinRoom.ReceivedRequest obj)
        {
            List<Int32> roomKeys = lobbyWriter.Data.Rooms;
            RoomJoinResponse payload = JoinRoom(obj.Payload, roomKeys);

            //To Update component.
            lobbyWriter.SendUpdate(new MdgSchema.Lobby.Lobby.Update
            {
                Rooms = roomKeys
            });
            MdgSchema.Lobby.Lobby.JoinRoom.Response response = new MdgSchema.Lobby.Lobby.JoinRoom.Response
            {
                Payload = payload,
                RequestId = obj.RequestId
            };
            lobbyCommandReceiver.SendJoinRoomResponse(response);
            if (payload.Joined)
            {
                lobbyWriter.SendJoinedRoomEvent(payload);
            }
        }

        private RoomJoinResponse JoinRoom(MdgSchema.Lobby.RoomJoinRequest payload, List<Int32> currentRooms)
        {
            bool isHunter = payload.Type == MdgSchema.Player.PlayerType.HUNTER;
            Room room = rooms[currentRooms[payload.RoomJoining]];
            if (room.IsRoomFilled() || (room.hunterJoined && isHunter))
            {
                return new RoomJoinResponse { Joined = false };
            }
            room.hunterJoined = isHunter; 
            room.playerCount += 1;
            currentRooms[payload.RoomJoining] = room.IsRoomFilled() ? 1 : 0;
            System.Guid guid = System.Guid.NewGuid();

            return new RoomJoinResponse {
                Joined = true,
                HunterJoined = room.hunterJoined,
                RoomSize = room.playerCount,
                ResponseId = guid.ToString()
            };
       }

    }
}