using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Should rename schema before this gets too big.
using MdgSchema.Player;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using MDG.Hunter.Systems;
using MDG.Common.Systems;
using MDG.Hunter.Systems.UnitCreation;
using MdgSchema.Spawners;
using Improbable.Worker.CInterop.Query;
using Improbable.Gdk.Core.Commands;
using Unity.Collections;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;

namespace MDG.ClientSide
{
    public class GameManager : MonoBehaviour
    {
        CommandSystem commandSystem;
        [SerializeField]
        private UserInterface.UIManager uiManager;

        [SerializeField]
        private Camera originalCamera;

        private long? playerRequestId;

        PlayerType playerSelected;
        // Start is called before the first frame update
        void Start()
        {
            uiManager.OnRoleSelected += CreatePlayer;

        }

        void CreatePlayer(PlayerType type)
        {
            UnityClientConnector connector = GetComponent<UnityClientConnector>();
            playerSelected = type;
            if (connector)
            {

                    var playerCreationSystem = connector.Worker.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();
                playerCreationSystem.RequestPlayerCreation(serializedArguments: DTO.Converters.SerializeArguments(new DTO.PlayerConfig
                {
                    playerType = type
                }), OnCreatePlayerResponse);
            }
            else
            {
                Debug.Log("COnnector null");
            }
            
        }

        //Move this and the creation requests to manager and just have this call it from manager.
        private void OnCreatePlayerResponse(PlayerCreator.CreatePlayer.ReceivedResponse response)
        {
            if (response.StatusCode != Improbable.Worker.CInterop.StatusCode.Success)
            {
                Debug.LogWarning($"Error: {response.Message}");
            }
            else
            {
                if (playerSelected == PlayerType.HUNTER)
                {
                    UnityClientConnector connector = GetComponent<UnityClientConnector>();
                    //Could have a system that initializes player on start up with the non spatialOS components.
                    //Spawn Units as well.
                    UnitCreationRequestSystem unitCreationSystem = connector.Worker.World.GetOrCreateSystem<UnitCreationRequestSystem>();
                }
            }
        }

        private void Update()
        {
           
        }
    }
}