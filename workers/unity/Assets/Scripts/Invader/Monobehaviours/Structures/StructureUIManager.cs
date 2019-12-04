using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.ScriptableObjects.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Invader.Monobehaviours.Structures
{
    public class StructureUIManager : MonoBehaviour
    {
        public StructureSchema.StructureType StructureType;
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
        }

        private void OnJobRun(int jobIndex, StructureSchema.JobRunEventPayload jobRunPayload)
        {
            float jobPct = jobRunPayload.JobProgress / (float)jobRunPayload.EstimatedJobCompletion;
            jobQueueUI[jobIndex].UpdateProgress(jobPct);
        }

        void OnFinishJob(int jobIndex, byte[] jobData)
        {
           // Not needed anymore since attached directly to oncoplete event of job slots.
        }


        private void DisplayErrorMessage(string errorMessage)
        {
            errorText.text = errorMessage;
        }
       
    }
}