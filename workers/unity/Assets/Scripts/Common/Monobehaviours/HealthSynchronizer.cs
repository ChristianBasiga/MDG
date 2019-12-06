using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StatSchema = MdgSchema.Common.Stats;


namespace MDG.Common.MonoBehaviours
{
    public class HealthSynchronizer : MonoBehaviour
    {
        public Image healthbar;
        // Make const later.
        [Require] StatSchema.StatsReader statsReader;

        [Require] StatSchema.StatsMetadataReader statsMetaDataReader;
        void Start()
        {
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnHealthUpdate(int newHealth)
        {
            OnHealthUpdate(newHealth, statsMetaDataReader.Data.Health);
        }

        void OnHealthUpdate(int health, int maxHealth)
        {
            float percentageHealth = health / (float)maxHealth;
            StartCoroutine(HelperFunctions.UpdateFill(healthbar, percentageHealth, (float pct) =>
            {
                if (pct == 0)
                {
                    Debug.Log("Do I not happen??");
                    this.gameObject.SetActive(false);
                    //this.statsReader.RemoveAllCallbacks();
                }
            }));
        }
    }

}