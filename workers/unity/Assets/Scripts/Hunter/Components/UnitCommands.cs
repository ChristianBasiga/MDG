using Improbable.Gdk.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace MDG.Hunter.Components
{
    //Add more to these as needed.
    public struct MoveCommand : IComponentData
    {
        public float3 destination;
    }

    public struct AttackCommand : IComponentData
    {
        public EntityId target;
    }

    public struct CollectCommand : IComponentData
    {
        public EntityId resourceId;
        public float3 destination;
    }
}