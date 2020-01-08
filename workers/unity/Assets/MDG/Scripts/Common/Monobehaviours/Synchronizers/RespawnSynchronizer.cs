using Improbable.Gdk.Subscriptions;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;

namespace MDG.Common.MonoBehaviours.Synchronizers
{
    public class RespawnSynchronizer : MonoBehaviour
    {
        [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader;
        // Start is called before the first frame update
        void Start()
        {
            pendingRespawnReader.OnRespawnActiveUpdate += PendingRespawnReader_OnRespawnActiveUpdate;
        }

        private void PendingRespawnReader_OnRespawnActiveUpdate(bool isRespawning)
        {
            Debug.Log("I happen?");
            this.gameObject.SetActive(!isRespawning);
        }
    }
}