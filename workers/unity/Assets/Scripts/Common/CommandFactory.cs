using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Game.Resource;
using Improbable.Gdk.Core;
using Unity.Entities;
using MDG.Common.Components;
namespace MDG.Hunter.Commands
{

    public struct CommandPending
    {
        //public ICommand Command;
        public Vector3 Dest;
        public EntityId TargetId;
        public CommandType CommandMetaData;
        public EntityId CommandListenerId;
    }

    //For now just static methods fine.
    //INstead of factory, CommandCreationSystem that listens for events
    // but if system then it's same issue, can't add entity dependancies if not main thread.
    public class CommandFactory
    {


        // So what I'll do is this will invoke event of command created.
        // GameManager will subscribe to this event, and then attach to entity accordingly.
        public delegate void CommandCreatedHandler(EntityId commandListenerId, ICommand command, CommandType commandMetadata);
        public static event CommandCreatedHandler OnCommandCreated;

        /*
        public static void SetCommand(EntityId commandListenerId,Vector3 dest)
        {
            Vector3 toWorldPoint = Camera.main.ScreenToWorldPoint(dest);

            OnCommandCreated?.Invoke(commandListenerId,  new MoveCommand { targetPosition = toWorldPoint, minDistance = 1 }, CommandType.Move);
        }
        
        public static CommandPending GetCommand(Vector3 dest)
        {
            Vector3 toWorldPoint = Camera.main.ScreenToWorldPoint(dest);

            return new CommandPending
            {
                Command = new MoveCommand { targetPosition = toWorldPoint, minDistance = 1 },
                CommandMetaData = CommandType.Move,
            };
        }


        /*
        public static void SetCommand(Metadata metadata, Vector3 dest)
        {
            Vector3 toWorldPoint = Camera.main.ScreenToWorldPoint(dest);

            switch (metadata.type) {
                case MetadataType.Resource:
                    
                    OnCommandCreated?.Invoke(commandListenerId, new CollectCommand
                    {
                        //Float will be from static dictionary of max distance.
                        GoToResource = new MoveCommand { targetPosition = toWorldPoint, minDistance = 1.2f }
                    }, CommandType.Collect);
                    break;
            }
        }

        //Return command based on Metadata of target to do action on.
        public static CommandPending GetCommand(Metadata metadata, Vector3 dest)
        {
            Vector3 toWorldPoint = Camera.main.ScreenToWorldPoint(dest);

            switch (metadata.type)
            {
                case MetadataType.Resource:
                    return new CommandPending
                    {
                        //Float will be from static dictionary of max distance.
                        Dest = toWorldPoint,
                        Command = new CollectCommand { GoToResource = new MoveCommand { targetPosition = toWorldPoint, minDistance = 1.2f } },
                        CommandMetaData = CommandType.Collect
                    };
                default:
                    return new CommandPending
                    {
                        CommandListenerId = new EntityId(-1),
                        TargetId = new EntityId(-1)
                        
                    };
            }
        }
        */
    }
}