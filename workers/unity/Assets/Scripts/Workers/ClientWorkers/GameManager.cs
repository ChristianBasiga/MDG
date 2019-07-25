using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Should rename schema before this gets too big.
using MdgSchema.Player;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;

namespace MDG.ClientSide
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private UserInterface.UIManager uiManager;


        // Start is called before the first frame update
        void Start()
        {
            uiManager.OnRoleSelected += CreatePlayer;

        }

        // Update is called once per frame
        void Update()
        {

        }


        void CreatePlayer(PlayerType type)
        {
            UnityClientConnector connector = GetComponent<UnityClientConnector>();

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
                Debug.LogError("Player Created");
            }
        }

    }
}