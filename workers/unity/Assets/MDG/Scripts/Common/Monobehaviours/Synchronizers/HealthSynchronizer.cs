﻿using Improbable.Gdk.Core;
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
#pragma warning disable 649
        public Image healthbar;
        [Require] StatSchema.StatsReader statsReader;
        [Require] StatSchema.StatsMetadataReader statsMetaDataReader;
#pragma warning restore 649
        public System.Action<int> OnHealthBarUpdated;

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
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HelperFunctions.UpdateFill(healthbar, percentageHealth, (float pct) =>
                {
                    int truncated = (int)pct;
                    // If no call back kill me.
                    if (truncated <= 0 && OnHealthBarUpdated == null)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        OnHealthBarUpdated?.Invoke(truncated);
                    }
                }));
            }
        }
    }

}