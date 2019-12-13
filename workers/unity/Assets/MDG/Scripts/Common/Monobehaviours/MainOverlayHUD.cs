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

        private void Start()
        {
            exitGameButton.gameObject.SetActive(false);
            endGameText.gameObject.SetActive(false);    
            gameStatusHUD.SetActive(false);
            // It's not great that gameobject unused exists, especially if fairly large.
     //       defenderHUD.gameObject.SetActive(false);
     //       invaderHUD.gameObject.SetActive(false);
        }

        private void Update()
        {
        }

        public void UpdatePoints(int points)
        {
            if (pointText == null)
            {
                pointText = GameObject.Find("PointText").GetComponent<Text>();
            }
            pointText.text = points.ToString();
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
            exitGameButton.gameObject.SetActive(true);
        }


        public void SelectRole(string role)
        {
            GameEntityTypes type = (GameEntityTypes) System.Enum.Parse(typeof(GameEntityTypes), role);
            OnRoleSelected?.Invoke(type);
            RoleSelected = type;
            StartCoroutine(TransitionUI());
        }

        IEnumerator TransitionUI()
        {
            roleSelectionUI.SetActive(false);
            UnityClientConnector unityClientConnector = GetComponent<UnityClientConnector>();
            yield return new WaitUntil(() => unityClientConnector.PlayerFinishedLoading);
            yield return new WaitForEndOfFrame();
            // Load in Loading screen until done loading.
            gameStatusHUD.SetActive(true);
            uiCamera.gameObject.SetActive(false);
        }
    }
}