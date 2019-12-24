using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using Improbable;
using MdgSchema.Common.Util;

namespace MDG.Invader.Components
{
    public struct CommandListener : IComponentData
    {
        public CommandType CommandType;
        public EntityId TargetId;
        public Vector3f TargetPosition;
    }

    // Atm used just as filter for only workers  in entity query.
    // atm no real reason for this to be a component.
    public struct WorkerUnit : IComponentData
    {

    }
}