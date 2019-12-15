using Improbable;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SpawnSchema = MdgSchema.Common.Spawn;
//For non authoritative client Units to update position accordingly.
public class UnitSynchronization : MonoBehaviour
{

    //[Require] SpawnSchema.PendingRespawnReader pendingRespawnReader = null;

    private void Start()
    {
        //  pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
        GetComponent<HealthSynchronizer>().OnHealthBarUpdated += OnHealthUpdated;
    }

    private void OnHealthUpdated(int pct)
    {
        Debug.Log("here??");
        if (pct == 0)
        {
            Destroy(gameObject);
        }
    }
}
