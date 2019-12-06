using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.MonoBehaviours
{
    public class EntityTransformSynchronizer : MonoBehaviour
    {
        [Require] EntityTransformReader transformReader;

        void Start()
        {
            transform.position = transformReader.Data.Position.ToUnityVector();
            transformReader.OnPositionUpdate += UpdatePosition;
            transformReader.OnRotationUpdate += UpdateRotation;
        }

        private void UpdateRotation(Improbable.Vector3f obj)
        {
           // transform.eulerAngles = obj.ToUnityVector();
        }

        private void UpdatePosition(Improbable.Vector3f obj)
        {
            transform.position = obj.ToUnityVector();
        }
    }
}