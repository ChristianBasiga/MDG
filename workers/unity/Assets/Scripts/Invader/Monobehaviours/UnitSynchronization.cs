using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SpawnSchema = MdgSchema.Common.Spawn;
//For non authoritative client Units to update position accordingly.
public class UnitSynchronization : MonoBehaviour
{

    [Require] EntityTransformReader positionReader = null;
    [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader = null;

    private void Start()
    {
        positionReader.OnPositionUpdate += PositionReader_OnPositionUpdate;
        pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
        
    }

    private void OnRespawnActiveChange(bool respawning)
    {
        gameObject.SetActive(!respawning);
    }

    private void PositionReader_OnPositionUpdate(Vector3f newPos)
    {
        transform.position = newPos.ToUnityVector();
    }

}
