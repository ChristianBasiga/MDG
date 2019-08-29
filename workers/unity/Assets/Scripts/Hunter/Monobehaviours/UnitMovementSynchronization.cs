using Improbable;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//For non authoritative client Units to update position accordingly.
public class UnitMovementSynchronization : MonoBehaviour
{

    [Require] PositionReader positionReader;
    NavMeshAgent agent;
    private void Start()
    {
        if (positionReader.IsValid)
        {
            positionReader.OnCoordsUpdate += SyncPosition;
        }
        agent = GetComponent<NavMeshAgent>();
    }

    // Need to happena after a frame, cause spawning position requires this update.
    private void Update()
    {
        //Ideally we actually move both by navmesh agent.
        Vector3 offset = positionReader.Data.Coords.ToUnityVector() - transform.position;
        agent.Move(offset * Time.deltaTime);

        transform.position = positionReader.Data.Coords.ToUnityVector();
    }

    private void SyncPosition(Coordinates coords)
    {
        transform.position = coords.ToUnityVector();
    }

    
}
