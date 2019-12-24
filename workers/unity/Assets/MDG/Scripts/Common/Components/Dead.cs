using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{
    // Tempoary component to trigger animation and allocate points.
    [RemoveAtEndOfTick]
    public struct JustDied: IComponentData
    {
        public EntityId KilledBy;
    }

    // Tempoary component to remove once respawned.
    public struct Dead: IComponentData
    {
        public EntityId KilledBy;
    }
}