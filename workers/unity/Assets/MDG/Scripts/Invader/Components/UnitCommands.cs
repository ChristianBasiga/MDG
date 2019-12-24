using Improbable;
using Improbable.Gdk.Core;
using MdgSchema.Common.Util;
using Unity.Entities;
using Unity.Mathematics;
using StructureSchema = MdgSchema.Common.Structure;
namespace MDG.Invader.Components
{

    /// <summary>
    /// All units have a command listener.
    /// Depending on command type for command listner it adds a specific command component
    /// to archtype of entity.
    /// Note: Look into how bad constantly changing archtype is to see if
    /// this removal and addition of component is good idea or not
    /// Temp ones make sense, but potentially bringing up new cache set.
    /// </summary>

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
        public Vector3f Destination;
        public bool Applied;
    }

    // Will get positon of target each time to follow.
    public struct AttackCommand : IComponentData
    {
        public EntityId Target;
        public bool Attacking;
    }

    // resource location won't change, so won't be checking position each frame
    public struct CollectCommand : IComponentData
    {
        public EntityId ResourceId;
        public Vector3f Destination;
        public bool IsAtResource;
        public bool IsCollecting;
        public bool GoingToResource;
    }

    public struct BuildCommand : IComponentData
    {
        public StructureSchema.StructureType StructureType;
        public Vector3f BuildLocation;
        public float MinDistanceToBuild;
        public bool IsBuilding;
        public int ConstructionTime;
        public EntityId StructureId;
        public EntityId BuilderId;
        public bool HasPendingBuildRequest;

        // For claims
        // Optionals within components is kinda sketch but spatial does it sothese use cases
        // make snse.
        public EntityId? TerritoryId;
    }

    [RemoveAtEndOfTick]
    public struct CommandInterrupt : IComponentData
    {
        public CommandType Interrupting;
        public EntityId? Target;
    }
}