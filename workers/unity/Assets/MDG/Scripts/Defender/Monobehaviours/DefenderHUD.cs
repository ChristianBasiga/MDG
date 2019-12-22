using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common;
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
#pragma warning disable 649

        [SerializeField]
        Text errorText;

        [SerializeField]
        Text pointText;

        [SerializeField]
        Image crossHairs;

        [SerializeField]
        Image playerHealthBar;


        [SerializeField]
        Text respawnTimer;
#pragma warning restore 649

        bool newErrorPassed = false;
        string text;
        IEnumerator errorClearRoutine;
        ClientGameObjectCreator clientGameObjectCreator;

        // Start is called before the first frame update
        void Start()
        {
            errorText.gameObject.SetActive(false);
            respawnTimer.transform.parent.gameObject.SetActive(false);
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

        public void OnRespawning()
        {
            respawnTimer.transform.parent.gameObject.SetActive(true);
        }

        public void OnUpdateRespawn(float time)
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
            respawnTimer.text = $"{minuteText}:{secondText}";
        }

        public void OnDoneRespawning()
        {
            respawnTimer.transform.parent.gameObject.SetActive(false);

        }

        public void OnUpdateHealth(float healthPct)
        {
            StartCoroutine(HelperFunctions.UpdateFill(playerHealthBar, healthPct));
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

        private void Update()
        {
            crossHairs.transform.position = Input.mousePosition;
        }
    }
}