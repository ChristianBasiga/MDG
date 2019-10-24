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
        Image healthbar;
        // Make const later.
        [SerializeField]
        float updateSpeedInSeconds = 2.2f;

        // Start is called before the first frame update
        void Start()
        {
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnHealthUpdate(int newHealth)
        {
            float percentageHealth = newHealth / (float)statsMetadataReader.Data.Health;
            StartCoroutine(HelperFunctions.UpdateHealthBar(healthbar, percentageHealth, updateSpeedInSeconds));
        }
    }
}