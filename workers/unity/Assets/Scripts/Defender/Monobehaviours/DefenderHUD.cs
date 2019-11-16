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

        // Start is called before the first frame update
        void Start()
        {
            pointText = GameObject.Find("PointText").GetComponent<Text>();
            pointReader.OnValueUpdate += UpdatePointText;
        }

        private void UpdatePointText(int pointValue)
        {
            pointText.text = pointValue.ToString();
        }
    }
}