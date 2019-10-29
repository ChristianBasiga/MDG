using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Common.MonoBehaviours
{
    // General Structure Panel all structure Panels will prob derive from.
    // Contains loading bar for comleteing job.
    // loading bar for completiting construction, etc.
    // Recieves Events via ComponentUpdateSystem and updates UI accordingly.
    // Specific of structure Panel will be it's on Monobehaviour used in composition with this.
    public class StructurePanel : MonoBehaviour
    {
        ComponentUpdateSystem componentUpdateSystem;
        LinkedEntityComponent linkedEntityComponent;
        [Require] StructureSchema.StructureReader structureReader;

        Image constructionProgressBar;
        Image jobProgressBar;
        // Start is called before the first frame update
        void Start()
        {
            structureReader.OnConstructingUpdate += OnConstructionUpdate;
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            componentUpdateSystem = linkedEntityComponent.World.GetExistingSystem<ComponentUpdateSystem>();
        }

        private void OnConstructionUpdate(bool constructed)
        {
            // Swap meshes. Do this later, unimportant. For now can just have a layer of opacity over structure.

        }

        // Update is called once per frame
        void Update()
        {
            // Actual actions done on construction and job completion will be done by systems.
            // This log is purely for UI.
            if (structureReader.Data.Constructing)
            {
                var buildEvents = componentUpdateSystem.GetEventsReceived<StructureSchema.Structure.Build.Event>(linkedEntityComponent.EntityId);
                for (int i = 0; i < buildEvents.Count; ++i)
                {
                    // I literally made both of them timers.
                    ref readonly var buildEvent = ref buildEvents[i];
                    float percentage = buildEvent.Event.Payload.BuildProgress / buildEvent.Event.Payload.EstimatedBuildTime;
                    HelperFunctions.UpdateFill(constructionProgressBar, percentage);
                    if (percentage == 1)
                    {
                        //Next frame, remove construction progress bar
                        StartCoroutine(OnFinishConstruction());
                    }

                }
            }
            else
            {
                var jobEvents = componentUpdateSystem.GetEventsReceived<StructureSchema.Structure.RunJob.Event>(linkedEntityComponent.EntityId);
                for (int i = 0; i < jobEvents.Count; ++i)
                {
                    ref readonly var jobEvent = ref jobEvents[i];
                    float percentage = jobEvent.Event.Payload.JobProgress / jobEvent.Event.Payload.EstimatedJobCompletion;
                    HelperFunctions.UpdateFill(jobProgressBar, percentage);
                    if (percentage == 1)
                    {
                        // Next frame, remove job progress bar
                        StartCoroutine(OnFinishJob());
                    }
                }
            }
          
        }

        IEnumerator ResetBar(Image bar)
        {
            yield return new WaitForEndOfFrame();
            bar.fillAmount = 0;
            bar.gameObject.SetActive(false);
        }

        IEnumerator OnFinishConstruction()
        {
            yield return ResetBar(constructionProgressBar);
            //Swap mesh, clear other UI.
        }

        IEnumerator OnFinishJob()
        {
            yield return ResetBar(jobProgressBar);
            // Clear other UI.
        }
    }
}