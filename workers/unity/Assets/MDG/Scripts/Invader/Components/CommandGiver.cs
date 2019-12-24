using Improbable.Gdk.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace MDG.Invader.Components
{
    // Maybe convert this to server side if I'm going to host multiple games sessions
    // in same world. That is only use case where this component still makes sense.
    [RemoveAtEndOfTick]
    public struct Selection : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float3 Scale;
    }
}