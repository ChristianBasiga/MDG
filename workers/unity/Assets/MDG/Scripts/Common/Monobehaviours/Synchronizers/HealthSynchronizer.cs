using Improbable.Gdk.Subscriptions;
using UnityEngine;
using UnityEngine.UI;
using StatSchema = MdgSchema.Common.Stats;


namespace MDG.Common.MonoBehaviours.Synchronizers
{
    public class HealthSynchronizer : MonoBehaviour
    {
#pragma warning disable 649
        public Image healthbar;
        [Require] StatSchema.StatsReader statsReader;
        [Require] StatSchema.StatsMetadataReader statsMetaDataReader;
#pragma warning restore 649
        public System.Action<float> OnUpdateHealth;
        public System.Action<float> OnHealthBarUpdated;
        public bool UpdatingHealh
        {
            private set;
            get;

        }

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
            OnUpdateHealth?.Invoke(percentageHealth);
            if (gameObject.activeInHierarchy)
            {
                UpdatingHealh = true;
                StartCoroutine(HelperFunctions.UpdateFill(healthbar, percentageHealth, (float pct) =>
                {
                    // If no call back kill me.
                    OnHealthBarUpdated?.Invoke(pct);
                    if (pct == 0 && OnHealthBarUpdated == null)
                    {
                       Destroy(gameObject);
                    }
                    UpdatingHealh = false;
                }));
            }
        }
    }

}