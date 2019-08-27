using Unity.Entities;
using MDG.Hunter.Commands;
using Improbable.Gdk.Core;
using UnityEngine;

namespace MDG.Hunter.Components
{
    public struct CommandListener : IComponentData
    {
        public CommandType CommandType;
        public EntityId TargetId;
        public Vector3 TargetPosition;
    }
}