using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MDG.Defender.Monobehaviours
{
    public class DefenderHUD : MonoBehaviour
    {
        [Require] PointReader pointReader;
        Text pointText;

        // Need to get this as singleton later.
        MainOverlayHUD uIManager;

        Text endGameText;

        // Start is called before the first frame update
        void Start()
        {
            uIManager = GameObject.Find("ClientWorker").GetComponent<MainOverlayHUD>();
            pointText = GameObject.Find("PointText").GetComponent<Text>();
            
            // UI Manager should attach 
            DefenderSynchronizer defenderSynchronizer = GetComponent<DefenderSynchronizer>();
            defenderSynchronizer.OnLoseGame += DisplayLoseGameUI;
            defenderSynchronizer.OnWinGame += DisplayWinGameUI;

            pointReader.OnValueUpdate += UpdatePointText;
        }

        private void UpdatePointText(int pointValue)
        {
            pointText.text = pointValue.ToString();
        }

        private void DisplayLoseGameUI()
        {
            uIManager.SetEndGameText("You failed to stop the invasion.", false);
        }

        private void DisplayWinGameUI()
        {
            uIManager.SetEndGameText("You have stopped the Invasion", true);
        }
    }
}