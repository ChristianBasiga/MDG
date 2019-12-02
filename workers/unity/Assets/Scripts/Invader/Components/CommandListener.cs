using Unity.Entities;
using MDG.Invader.Commands;
using Improbable.Gdk.Core;
using UnityEngine;
using Improbable;

namespace MDG.Invader.Components
{
    public struct CommandListener : IComponentData
    {
        public CommandType CommandType;
        public EntityId TargetId;
        public Vector3f TargetPosition;
    }

    public struct WorkerUnit : IComponentData
    {

    }
}