using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Core;
namespace MDG.Common.Components
{
    public struct MouseInputComponent : IComponentData
    {
        public bool DidClickThisFrame;
        public bool LeftClick;
        public bool RightClick;
        public Vector3 LastClickedPos;
        public EntityId SelectedEntityId;
    }
}