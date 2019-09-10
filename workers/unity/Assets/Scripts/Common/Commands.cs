using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine.AI;
namespace MDG.Hunter.Commands
{
    //Perhaps Commands should be Components, can add and remove components from Entity at run time dude.
    //AKA perfect for these. I'm retarded.
   
    public enum CommandType
    {
        None,
        Move,
        Collect,
        Attack
    }

    //Move all this to command
    public interface ICommand: IComponentData
    {
        bool DoneExecuting();
        void Execute(GameObject gameObject);
    }

    //This will be part of Unit Archtype / template with command Type None.
    public struct CommandMetadata : IComponentData
    {
        public CommandType CommandType;
        public EntityId TargetId;
        public Vector3 TargetPosition;
    }
}