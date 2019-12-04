using Improbable;
using Improbable.Gdk.Core;
using Unity.Entities;
using Unity.Mathematics;
using StructureSchema = MdgSchema.Common.Structure;
namespace MDG.Invader.Components
{

    public enum CommandType
    {
        None,
        Move,
        Collect,
        Attack,
        Build
    }
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

    public struct BuildCommand : IComponentData
    {
        public StructureSchema.StructureType structureType;
        public Vector3f buildLocation;
        public float minDistanceToBuild;
        public bool isBuilding;
        public int constructionTime;
        public EntityId structureId;
        public EntityId builderId;
        public bool hasPendingBuildRequest;
    }

    [RemoveAtEndOfTick]
    public struct CommandInterrupt : IComponentData
    {
        public CommandType interrupting;
        public EntityId? target;
    }
}