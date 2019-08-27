using Improbable;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//For non authoritative client Units to update position accordingly.
public class UnitMovementSynchronization : MonoBehaviour
{

    [Require] PositionReader positionReader;

    private void Start()
    {
        if (positionReader.IsValid)
        {
            positionReader.OnCoordsUpdate += SyncPosition;
        }
    }

    // Need to happena after a frame, cause spawning position requires this update.
    private void Update()
    {
        transform.position = positionReader.Data.Coords.ToUnityVector();
    }

    private void SyncPosition(Coordinates coords)
    {
        transform.position = coords.ToUnityVector();
    }

    
}
