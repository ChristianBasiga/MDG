using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using UnityEngine.UI;
using MDG.Common;


namespace MDG.Invader.Monobehaviours
{
    public class UnitHoverUI : MonoBehaviour
    {
        [Require] StatSchema.StatsReader statsReader;
        // This shouldn't ever be changing, but it's fine.
        [Require] StatSchema.StatsMetadataReader statsMetadataReader;


        public Image healthbar;
        // Make const later.
        [SerializeField]
        float updateSpeedInSeconds = 1.0f;

        // Start is called before the first frame update
        void Start()
        {
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnHealthUpdate(int newHealth)
        {
            // Gotta update these so that health goes down fully before gets fully deleted.
            float percentageHealth = newHealth / (float)statsMetadataReader.Data.Health;
            StartCoroutine(HelperFunctions.UpdateHealthBar(healthbar, percentageHealth, updateSpeedInSeconds));
        }
    }
}