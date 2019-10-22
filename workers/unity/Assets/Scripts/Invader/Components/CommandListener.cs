using Unity.Entities;
using MDG.Invader.Commands;
using Improbable.Gdk.Core;
using UnityEngine;

namespace MDG.Invader.Components
{
    public struct CommandListener : IComponentData
    {
        public CommandType CommandType;
        public EntityId TargetId;
        public Vector3 TargetPosition;
    }
}