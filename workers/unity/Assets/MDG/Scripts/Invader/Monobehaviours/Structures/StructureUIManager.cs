using Improbable.Gdk.Subscriptions;
using MDG.Invader.Monobehaviours.UserInterface;
using MDG.ScriptableObjects.Items;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Invader.Monobehaviours.Structures
{
    public class StructureUIManager : MonoBehaviour
    {
        public StructureSchema.StructureType StructureType;


        public BuildMenu buildMenu;
        public Text errorText;
        public JobSlot[] jobQueueUI;
        public StructureBehaviour structureBehaviour;

        

        public void SetStructure(StructureBehaviour newStructureBehaviour)
        {
            if (structureBehaviour != null)
            {
                structureBehaviour.OnJobStarted -= OnJobStarted;
                structureBehaviour.OnJobRun -= OnJobRun;
                structureBehaviour.OnJobCompleted -= OnFinishJob;
                structureBehaviour.OnError -= DisplayErrorMessage;

            }
            structureBehaviour = newStructureBehaviour;
            structureBehaviour.OnJobStarted += OnJobStarted;
            structureBehaviour.OnJobRun += OnJobRun;
            structureBehaviour.OnJobCompleted += OnFinishJob;
            structureBehaviour.OnError += DisplayErrorMessage;
        }

        public void SetJobs(ShopItem[] jobs)
        {
        }

        private void Start()
        {
            errorText.text = "";
            int queueLength = jobQueueUI.Length;
            for (int i = 0; i < queueLength; ++i)
            {
                // For closure
                int jobIndex = i;
                jobQueueUI[jobIndex].OnJobCompleted += (_) =>
                {
                    jobQueueUI[jobIndex].ClearJob();
                };
            }
        }


        private void OnJobStarted(int jobIndex, ShopItem jobInfo, LinkedEntityComponent arg2)
        {
            jobQueueUI[jobIndex].SetJob(jobInfo);
            errorText.text = "";
        }

        private void OnJobRun(int jobIndex, StructureSchema.JobRunEventPayload jobRunPayload)
        {
            if (this.gameObject.activeInHierarchy)
            {
                float jobPct = jobRunPayload.JobProgress / (float)jobRunPayload.EstimatedJobCompletion;
                jobQueueUI[jobIndex].UpdateProgress(jobPct);
            }
        }

        void OnFinishJob(int jobIndex, byte[] jobData)
        {
           // Not needed anymore since attached directly to oncoplete event of job slots.
        }


        // Actually should just put this in a timer.
        private void DisplayErrorMessage(string errorMessage)
        {
            errorText.text = errorMessage;
            StartCoroutine(FadeOutErrorMessage());
        }

        IEnumerator FadeOutErrorMessage()
        {
            string originalText = errorText.text;

            float time = 0;
            float duration = 2.0f;
            while (time < duration)
            {
                if (errorText.text != originalText)
                {
                    yield break;
                }
                time += Time.deltaTime;
                yield return null;
            }
            errorText.text = "";
        }
       
    }
}