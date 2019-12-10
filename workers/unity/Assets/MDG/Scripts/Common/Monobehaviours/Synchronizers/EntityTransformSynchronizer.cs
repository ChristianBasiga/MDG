using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.MonoBehaviours
{
    public class EntityTransformSynchronizer : MonoBehaviour
    {
#pragma warning disable 649
        [Require] EntityPositionReader positionReader;
        [Require] EntityRotationReader rotationReader;
#pragma warning restore 649

        void Start()
        {
            transform.position = HelperFunctions.Vector3fToVector3(positionReader.Data.Position);
            positionReader.OnPositionUpdate += UpdatePosition;
            rotationReader.OnRotationUpdate += UpdateRotation;            
        }

        private void UpdateRotation(Vector3f obj)
        {
            transform.eulerAngles = HelperFunctions.Vector3fToVector3(obj);
        }

        private void UpdatePosition(Vector3f obj)
        {
            transform.position = HelperFunctions.Vector3fToVector3(obj);
        }
    }
}