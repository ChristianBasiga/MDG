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
        [Require] StatSchema.StatsReader statsReader = null;
        [Require] StatSchema.StatsMetadataReader statsMetadataReader = null;


        Image constructionProgressUI;
        Image healthUI;

        // Start is called before the first frame update
        void Start()
        {
            Transform structureHud = transform.Find("StructureHUD");
            constructionProgressUI = structureHud.Find("Healthbar").GetComponent<Image>();
            healthUI = structureHud.Find("ConstructionProgress").GetComponent<Image>();

            structureReader.OnBuildingEvent += UpdateConstructionProgress;
            statsReader.OnHealthUpdate += UpdateHealthBar;
        }

        private void UpdateHealthBar(int health)
        {
            float pct = health / (float)statsMetadataReader.Data.Health;
            StartCoroutine(HelperFunctions.UpdateFill(healthUI, pct));
        }

        private void UpdateConstructionProgress(StructureSchema.BuildEventPayload  buildEventPayload)
        {
            // Idk how many times repeat this shit lol, but it's all temp, bar easiest way to show progress.
            float pct = buildEventPayload.BuildProgress / (float)buildEventPayload.EstimatedBuildCompletion;
            StartCoroutine(HelperFunctions.UpdateFill(constructionProgressUI, pct));
        }
    }
}