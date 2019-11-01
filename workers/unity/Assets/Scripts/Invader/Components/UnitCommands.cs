using Improbable;
using Improbable.Gdk.Core;
using MDG.Invader.Commands;
using Unity.Entities;
using Unity.Mathematics;

namespace MDG.Invader.Components
{
    //Add more to these as needed.
    public struct MoveCommand : IComponentData
    {
        public Vector3f destination;
        public bool applied;
    }

    // Will get positon of target each time to follow.
    public struct AttackCommand : IComponentData
    {
        public EntityId target;
        public bool attacking;
    }

    // resource location won't change, so won't be checking position each frame
    public struct CollectCommand : IComponentData
    {
        public EntityId resourceId;
        public Vector3f destination;
        public bool IsAtResource;
        public bool IsCollecting;
        public bool GoingToResource;
    }

    [RemoveAtEndOfTick]
    public struct CommandInterrupt : IComponentData
    {
        public CommandType interrupting;
        public EntityId? target;
    }
}