using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Player;
using MdgSchema.Common;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;

namespace MDG.ClientSide.UserInterface
{
   
    public class UIManager : MonoBehaviour
    {
        GameObject roleSelectionUI;

        public delegate void RoleSelectedHandler(GameEntityTypes type);
        public event RoleSelectedHandler OnRoleSelected;
        private EntityId gameManagerEntityId = new EntityId(3); 

        private void Start()
        {
            roleSelectionUI = GameObject.Find("RoleSelectionUI");
        }

        public void SelectRole(string role)
        {
            GameEntityTypes type = (GameEntityTypes) System.Enum.Parse(typeof(GameEntityTypes), role);
            OnRoleSelected?.Invoke(type);

            UnityClientConnector clientConnector = GetComponent<UnityClientConnector>();
            var playerCreationSystem = clientConnector.Worker.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();
            playerCreationSystem.RequestPlayerCreation(serializedArguments: DTO.Converters.SerializeArguments(new DTO.PlayerConfig
            {
                playerType = type,
            }), OnCreatePlayerResponse);




            var commandSystem = clientConnector.Worker.World.GetOrCreateSystem<CommandSystem>();
            commandSystem.SendCommand(new MdgSchema.Game.GameStatus.StartGame.Request
            {
                TargetEntityId = gameManagerEntityId,
                Payload = new MdgSchema.Game.StartGameRequest()
            });
            roleSelectionUI.SetActive(false);
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
                Debug.Log("Created player succssfully");
            }
        }
    }
}