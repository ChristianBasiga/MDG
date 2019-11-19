using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Player;
using MdgSchema.Common;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine.UI;
namespace MDG.Common.MonoBehaviours
{

    // Instead of UI Manager this is really MainOverlayUI.
    public class MainOverlayHUD : MonoBehaviour
    {
        GameObject roleSelectionUI;
        GameObject gameStatusHUD;
        Text endGameText;
        Text timerText;
        public delegate void RoleSelectedHandler(GameEntityTypes type);
        public event RoleSelectedHandler OnRoleSelected;
        private EntityId gameManagerEntityId = new EntityId(3);




        private void Start()
        {
            roleSelectionUI = GameObject.Find("RoleSelectionUI");
            gameStatusHUD = GameObject.Find("GameStatusHUD");
            endGameText = gameStatusHUD.transform.GetChild(0).Find("EndGameText").GetComponent<Text>();
            timerText = gameStatusHUD.transform.GetChild(0).Find("Timer").GetComponent<Text>();
            endGameText.gameObject.SetActive(false);    
            gameStatusHUD.SetActive(false);
        }

        public void UpdateTime(float time)
        {
            // Get minutes and remaining time after remving minutes
            int minutes = (int)(time / 60);
            int seconds = (int)(time - (minutes * 60));

            string minuteText = minutes.ToString();
            if (minutes / 10 == 0)
            {
                minuteText = "0" + minuteText;
            }
            string secondText = seconds.ToString();
            if (seconds / 10 == 0)
            {
                secondText = "0" + secondText;
            }

            timerText.text = $"{minuteText}:{secondText}";
        }

        public void SetEndGameText(string text, bool won)
        {
            endGameText.text = text;
            endGameText.color = won ? Color.blue : Color.red;
            endGameText.gameObject.SetActive(true);
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
            roleSelectionUI.SetActive(false);
            gameStatusHUD.SetActive(true);
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