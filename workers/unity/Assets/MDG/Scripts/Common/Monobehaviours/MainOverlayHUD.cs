using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Common;
using Improbable.Gdk.Core;
using UnityEngine.UI;

namespace MDG.Common.MonoBehaviours
{

    // Instead of UI Manager this is really MainOverlayUI.
    public class MainOverlayHUD : MonoBehaviour
    {


#pragma warning disable 649

        // Player HUDS

        [SerializeField]
        Canvas defenderHUD;

        [SerializeField]
        Canvas invaderHUD;

        [SerializeField]
        Camera uiCamera;

        [SerializeField]
        GameObject roleSelectionUI;

        [SerializeField]
        GameObject gameStatusHUD;

        [SerializeField]
        Button exitGameButton;

        [SerializeField]
        Text pointText;

        [SerializeField]
        Text endGameText;

        [SerializeField]
        Text timerText;
#pragma warning restore 649


        public delegate void RoleSelectedHandler(GameEntityTypes type);
        public event RoleSelectedHandler OnRoleSelected;
        public GameEntityTypes RoleSelected { private set; get; }


        public void SelectRole(string role)
        {
            GameEntityTypes type = (GameEntityTypes)System.Enum.Parse(typeof(GameEntityTypes), role);
            OnRoleSelected?.Invoke(type);
            RoleSelected = type;
            StartCoroutine(TransitionUI());
        }

        private void Start()
        {
            exitGameButton.gameObject.SetActive(false);
            endGameText.gameObject.SetActive(false);
            gameStatusHUD.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                exitGameButton.gameObject.SetActive(exitGameButton.gameObject.activeInHierarchy);
            }
        }


        private void UpdatePoints(int points)
        {
            if (pointText == null)
            {
                pointText = GameObject.Find("PointText").GetComponent<Text>();
            }
            pointText.text = points.ToString();
        }

        private void UpdateTime(float time)
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

        private void SetEndGameText(string text, bool won)
        {
            endGameText.text = text;
            endGameText.color = won ? Color.blue : Color.red;
            endGameText.gameObject.SetActive(true);
            exitGameButton.gameObject.SetActive(true);
        }

        IEnumerator TransitionUI()
        {
            roleSelectionUI.SetActive(false);
            UnityClientConnector unityClientConnector = GetComponent<UnityClientConnector>();
            yield return new WaitUntil(() => unityClientConnector.PlayerFinishedLoading);

            // Assign UI callbacks.

            GameStatusSynchronizer gameStatusSynchronizer = gameObject.AddComponent<GameStatusSynchronizer>();
            gameStatusSynchronizer.OnWinGame += (string text) =>
            {
                SetEndGameText(text, true);
            };
            gameStatusSynchronizer.OnLoseGame += (string text) =>
            {
                SetEndGameText(text, false);
            };
            gameStatusSynchronizer.OnUpdateTime += UpdateTime;
            PointSynchonizer pointSynchonizer = unityClientConnector.ClientGameObjectCreator.PlayerLink.GetComponent<PointSynchonizer>();
            pointSynchonizer.OnPointUpdate += UpdatePoints;
            yield return new WaitForEndOfFrame();
            // Load in Loading screen until done loading.
            gameStatusHUD.SetActive(true);
            uiCamera.gameObject.SetActive(false);
        }
    }
}