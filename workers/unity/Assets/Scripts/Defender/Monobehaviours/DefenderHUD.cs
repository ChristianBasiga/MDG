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
        [Require] StatSchema.StatsReader statsReader = null;
        [Require] StatSchema.StatsMetadataReader statsMetaDataReader = null;


        // Need to get this as singleton later.
        MainOverlayHUD mainOverlayHUD;

        // UI varaibles
        [SerializeField]
        Image healthBar;

        [SerializeField]
        Text errorText;

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
            statsReader.OnHealthUpdate += UpdateHealthBar;
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
            else
            {
                errorClearRoutine.Reset();
            }

        }

        IEnumerator ClearError()
        {
            yield return new WaitForSeconds(3.0f);
            errorText.gameObject.SetActive(false);
            errorClearRoutine = null;
        }


        void UpdateHealthBar(int health)
        {
            StartCoroutine(HelperFunctions.UpdateFill(healthBar, health / statsMetaDataReader.Data.Health));
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