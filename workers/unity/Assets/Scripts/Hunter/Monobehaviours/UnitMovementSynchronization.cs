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


    private void Update()
    {
        transform.position = positionReader.Data.Position.ToUnityVector();
    }

}
