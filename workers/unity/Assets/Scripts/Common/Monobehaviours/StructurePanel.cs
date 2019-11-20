using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.ScriptableObjects.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Common.MonoBehaviours.Structures
{
    public class StructurePanel : MonoBehaviour
    {
        public Text errorText;
        public Sprite blankJobIcon;
        public Image constructionProgressBar;
        public Image jobProgressBar;

        protected ShopItem[] jobQueue;

        public Image[] jobQueueUI;
        public StructureBehaviour structureBehaviour;

        // Start is called before the first frame update
        void Start()
        {
            structureBehaviour.OnJobStarted += OnJobStarted;
            structureBehaviour.OnJobRun += 
            structureBehaviour.OnBuildComplete += OnFinishConstruction;
            structureBehaviour.OnJobCompleted += StructureBehaviour_OnJobCompleted;
            structureBehaviour.OnError += DisplayErrorMessage;
        }

        private void OnJobStarted(int jobIndex, ShopItem jobInfo, LinkedEntityComponent arg2)
        {
            jobQueueUI[jobIndex].sprite = jobInfo.ArtWork;
        }

        private void OnJobRun(StructureSchema.JobRunEventPayload jobRunPayload)
        {
            StartCoroutine(HelperFunctions.UpdateFill(jobProgressBar, jobRunPayload.JobProgress / jobRunPayload.EstimatedJobCompletion));
        }

        private void DisplayErrorMessage(string errorMessage)
        {
            errorText.text = errorMessage;
        }

        IEnumerator ResetBar(Image bar)
        {
            yield return new WaitForEndOfFrame();
            bar.fillAmount = 0;
            bar.gameObject.SetActive(false);
        }

        void OnFinishConstruction()
        {
            //Swap mesh, clear other UI.
            StartCoroutine(ResetBar(constructionProgressBar));
        }


        void OnFinishJob(int jobIndex, byte[] jobData)
        {
            StartCoroutine(ResetBar(jobProgressBar));
            // set image.
            jobQueueUI[jobIndex].sprite = blankJobIcon;
        }

    }
}