using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StatSchema = MdgSchema.Common.Stats;
namespace MDG.Defender.Monobehaviours
{
    public class DefenderHUD : MonoBehaviour
    {

        // Component Readers
        [Require] PointReader pointReader = null;

        MainOverlayHUD mainOverlayHUD;


        [SerializeField]
        Text errorText;

        bool newErrorPassed = false;
        string text;
        IEnumerator errorClearRoutine;


        // Start is called before the first frame update
        void Start()
        {

            mainOverlayHUD = GameObject.Find("ClientWorker").GetComponent<MainOverlayHUD>();
            // Subsribe to main overlay hud.
            DefenderSynchronizer defenderSynchronizer = GetComponent<DefenderSynchronizer>();
            defenderSynchronizer.OnLoseGame += DisplayLoseGameUI;
            defenderSynchronizer.OnWinGame += DisplayWinGameUI;
            pointReader.OnValueUpdate += mainOverlayHUD.UpdatePoints;
            errorText.gameObject.SetActive(false);
        }


        // Need more granular way to do this.
        public void SetErrorText(string errorMsg)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = errorMsg;
            if (errorClearRoutine == null)
            {
                errorClearRoutine = ClearError();
                StartCoroutine(errorClearRoutine);
            }
        }

        IEnumerator ClearError()
        {
            float timePassed = 0;
            while (timePassed < 1.0f)
            {
                timePassed += Time.deltaTime;
                if (newErrorPassed)
                {
                    timePassed = 0;
                }
                yield return null;
            }
            errorText.gameObject.SetActive(false);
            errorClearRoutine = null;
        }



        private void DisplayLoseGameUI()
        {
            mainOverlayHUD.SetEndGameText("You failed to stop the invasion.", false);
        }

        private void DisplayWinGameUI()
        {
            mainOverlayHUD.SetEndGameText("You have stopped the Invasion", true);
        }

    }
}