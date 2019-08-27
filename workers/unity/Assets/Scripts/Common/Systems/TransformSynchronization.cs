using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable;

/// <summary>
/// This system manages all moving entities and makes sure it is synced up across all clients.
/// This is a server worker that receives updates from respective client workers. Monobehaviour to System.
/// Need to review SpatialOS to implement this. But firs get movement going period.
/// </summary>
public class TransformSynchronization : ComponentSystem
{
    EntityQuery filter;
    protected override void OnCreate()
    {
    }

    protected override void OnUpdate()
    {
       // throw new System.NotImplementedException();
    }
}
