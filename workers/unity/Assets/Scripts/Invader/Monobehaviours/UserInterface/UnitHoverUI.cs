using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using UnityEngine.UI;
using MDG.Common;
using MDG.Common.MonoBehaviours;

namespace MDG.Invader.Monobehaviours
{
    public class UnitHoverUI : HealthSynchronizer
    {
        public Image healthbar;
        // Make const later.
        [SerializeField]
        float updateSpeedInSeconds = 1.0f;
        [Require] StatSchema.StatsReader statsReader = null;

        [Require] StatSchema.StatsMetadataReader statsMetaDataReader = null;


        protected override void Start()
        {
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnHealthUpdate(int newHealth)
        {
            OnHealthUpdate(newHealth, statsMetaDataReader.Data.Health);
        }

        protected override void OnHealthUpdate(int health, int maxHealth)
        {
            float percentageHealth = health / (float)maxHealth;
            StartCoroutine(HelperFunctions.UpdateFill(healthbar, percentageHealth,(float pct) =>
            {
                Debug.Log("pct" + pct);
                if (pct >= 1)
                {
                    this.gameObject.SetActive(false);
                }
            },updateSpeedInSeconds));
        }
    }
}