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

    public struct MoveCommand : ICommand
    {
        //This works, but another component called metadata better as this lets me switch on assigning
        //I would need to somehow switch again which isn't possible in retrieving component.
        public Vector3 targetPosition;
        public Vector3 currentPosition;
        //Min distance before considered there, changes depending on moving to.
        public float minDistance;
        public bool DoneExecuting()
        {
            float distance = Mathf.Sqrt(Mathf.Pow(targetPosition.x - currentPosition.x, 2) + Mathf.Pow(targetPosition.z - currentPosition.z, 2));
            return distance < minDistance;
        }

        public void Execute(GameObject gameObject)
        {
            if (DoneExecuting()) return;
            targetPosition.y = gameObject.transform.position.y;
            NavMeshAgent navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            navMeshAgent.Move((targetPosition - gameObject.transform.position) * Time.deltaTime);
        }
    }

    // Should be EntityId of Resource.
    // Maybe Interactable Id will be type.
    // For now simply, the EntityId of the resource.
    //Stack of commands, if interrupted by another command, this will pause, then continue.
    // No time for all that.
    public struct CollectCommand : ICommand
    {
        public EntityId ResourceId;
        //With this in mind, could make a Command Builder Instead.
        public MoveCommand GoToResource;

        public bool DoneExecuting()
        {
            //If made it to resource, start collecting
            if (GoToResource.DoneExecuting())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       
        public void Execute(GameObject gameObject)
        {
            if (!GoToResource.DoneExecuting())
            {
                GoToResource.Execute(gameObject);
            }
            else
            {
                //This will simply collect, load balancing and such will simply be changing the GoToResource command portion of this.
                // as well as possible resource Id.
                log4net.LogManager.GetLogger(typeof(MonoBehaviour)).Debug("Collecting");
            }
        }
    }
}