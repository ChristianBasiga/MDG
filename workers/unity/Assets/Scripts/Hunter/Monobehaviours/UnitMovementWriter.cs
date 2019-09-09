using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Hunter.Monobehaviours
{
    // Rename this to writer in general for updating stuff like animation playing, etc.
    //
    public class UnitMovementWriter : MonoBehaviour
    {
        [Require] EntityTransformWriter positionWriter;
        public void UpdatePosition(Vector3 position)
        {
            positionWriter.SendUpdate(new EntityTransform.Update
            {
                Position = Vector3f.FromUnityVector(position)
            });
        }
    }
}