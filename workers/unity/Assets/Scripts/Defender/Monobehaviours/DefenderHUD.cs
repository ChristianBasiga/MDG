using Improbable.Gdk.Subscriptions;
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


        Text endGameText;

        // Start is called before the first frame update
        void Start()
        {
            pointText = GameObject.Find("PointText").GetComponent<Text>();
            endGameText = GameObject.Find("EndGameText").GetComponent<Text>();

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
            endGameText.text = "You lost. Invader has won.";
            endGameText.color = Color.red;
        }

        private void DisplayWinGameUI()
        {
            endGameText.text = "You won. You have stopped the Invasion";
            endGameText.color = Color.green;
        }
    }
}