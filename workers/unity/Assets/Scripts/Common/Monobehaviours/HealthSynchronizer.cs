using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;


namespace MDG.Common.MonoBehaviours
{
    public class HealthSynchronizer : MonoBehaviour
    {
        [Require] StatSchema.StatsReader statsReader;
        [Require] StatSchema.StatsMetadataReader statsMetaDataReader;

        protected virtual void Start()
        {
            Debug.Log(statsReader);
        }

        IEnumerator AttachHealthUpdateCallBack()
        {
            yield return new WaitUntil( () => statsReader != null && statsReader.IsValid);
            Debug.Log("Ever here??");
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnHealthUpdate(int health)
        {
            OnHealthUpdate(health, statsMetaDataReader.Data.Health);
        }
        protected virtual void OnHealthUpdate(int health, int maxHealth) { }
    }
}