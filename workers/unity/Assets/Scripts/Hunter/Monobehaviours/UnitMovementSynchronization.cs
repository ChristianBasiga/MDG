using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//For non authoritative client Units to update position accordingly.
public class UnitMovementSynchronization : MonoBehaviour
{

    [Require] EntityTransformReader positionReader;

    private void Start()
    {
        positionReader.OnPositionUpdate += PositionReader_OnPositionUpdate;
        
    }

    private void PositionReader_OnPositionUpdate(Vector3f newPos)
    {
        transform.position = newPos.ToUnityVector();
    }

}
