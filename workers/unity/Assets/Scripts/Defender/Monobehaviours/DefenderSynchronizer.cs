using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;

namespace MDG.Defender.Monobehaviours
{
    // Finish synchronization of defender.
    public class DefenderSynchronizer : MonoBehaviour
    {
        [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader;

        private void Start()
        {
            pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;

            // Soo how should bullets be handled in ecs? I'm pretty sure they HAVE to be tempaltes.

        }

        private void OnRespawnActiveChange(bool respawning)
        {
            // Trigger other things.
            Debug.Log("respawning " + respawning);
            gameObject.SetActive(!respawning);
        }
    }
}