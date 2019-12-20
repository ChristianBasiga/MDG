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
#pragma warning restore 649

        bool newErrorPassed = false;
        string text;
        IEnumerator errorClearRoutine;
        ClientGameObjectCreator clientGameObjectCreator;

        // Start is called before the first frame update
        void Start()
        {
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