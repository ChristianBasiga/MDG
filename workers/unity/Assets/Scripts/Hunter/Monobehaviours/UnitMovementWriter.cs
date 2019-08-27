using Improbable;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Hunter.Monobehaviours
{
    // Rename this to writer in general for updating stuff like animation playing, etc.
    //
    public class UnitMovementWriter : MonoBehaviour
    {
        [Require] PositionWriter positionWriter;
        public void UpdatePosition(Vector3 position)
        {
            positionWriter.SendUpdate(new Position.Update
            {
                Coords = new Coordinates { X = transform.position.x, Y = transform.position.y, Z = transform.position.z }
            });
        }
    }
}