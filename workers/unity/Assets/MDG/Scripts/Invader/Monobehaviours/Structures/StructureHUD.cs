using Improbable.Gdk.Subscriptions;
using MDG.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using StructureSchema = MdgSchema.Common.Structure;
using StatSchema = MdgSchema.Common.Stats;
namespace MDG.Invader.Monobehaviours.Structures {

    public class StructureHUD : MonoBehaviour
    {
        [Require] StructureSchema.StructureReader structureReader = null;


        Image constructionProgressUI;

        // Start is called before the first frame update
        void Start()
        {
            Transform structureHud = transform.Find("StructureHUD");
            constructionProgressUI = structureHud.Find("ConstructionProgress").GetComponent<Image>();

            structureReader.OnBuildingEvent += UpdateConstructionProgress;
        }


        private void UpdateConstructionProgress(StructureSchema.BuildEventPayload  buildEventPayload)
        {
            float pct = buildEventPayload.BuildProgress / (float)buildEventPayload.EstimatedBuildCompletion;
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HelperFunctions.UpdateFill(constructionProgressUI, pct, OnFillUpdated, 1, () => !this.gameObject.activeInHierarchy));
            }
            else
            {
                constructionProgressUI.fillAmount = pct;
            }
        }

        private void OnFillUpdated(float pct)
        {
            if ((int)pct == 1)
            {
                constructionProgressUI.gameObject.SetActive(false);
            }
        }
    }
}