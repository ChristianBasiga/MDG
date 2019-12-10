using Improbable;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
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

    [Require] EntityPositionReader positionReader = null;
    //[Require] SpawnSchema.PendingRespawnReader pendingRespawnReader = null;

    private void Start()
    {
        positionReader.OnPositionUpdate += PositionReader_OnPositionUpdate;
      //  pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
        
    }

    private void OnRespawnActiveChange(bool respawning)
    {
        gameObject.SetActive(!respawning);
    }

    private void PositionReader_OnPositionUpdate(Vector3f newPos)
    {
        transform.position = HelperFunctions.Vector3fToVector3(newPos);
    }

}
