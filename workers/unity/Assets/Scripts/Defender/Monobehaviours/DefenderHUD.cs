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

        // Need to get this as singleton later.
        MainOverlayHUD uIManager;


        // Start is called before the first frame update
        void Start()
        {
            // Subsribe to main overlay hud.
            DefenderSynchronizer defenderSynchronizer = GetComponent<DefenderSynchronizer>();
            defenderSynchronizer.OnLoseGame += DisplayLoseGameUI;
            defenderSynchronizer.OnWinGame += DisplayWinGameUI;

            pointReader.OnValueUpdate += uIManager.UpdatePoints;
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