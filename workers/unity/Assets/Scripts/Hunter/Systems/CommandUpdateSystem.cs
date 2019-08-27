using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Improbable.Gdk.Core;
using MDG.Hunter.Monobehaviours;
using System;
using MDG.Common.Systems;
using MDG.Logging;
namespace MDG.Hunter.Systems
{
    /// <summary>
    ///  This will be flow, swtich on meta data of command listener.
    ///  Based on switch, it will key to the command monobehaviour.
    ///  the monobehaviour will hold tasks for Behaviour tree, each command is more of a single task rather than a tree.
    ///  But I suppose the process and acting accorindgly could become a behaviour tree.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(CommandGiveSystem))]
    public class CommandUpdateSystem : JobComponentSystem
    {
        private bool assignedJobHandle = false;
        public JobHandle CommandExecuteJobHandle { get; private set; }
        //Component Type
        //Perhaps should rename to meta data or payload.
        public delegate void CommandUpdateEventHandler(EntityId commandListener, System.Type type, CommandListener commandPayload);
        //Each unit should be abel to get GameObject attached to via SpatialEntityId
        public static event CommandUpdateEventHandler OnCommandExecute;
        public static event CommandUpdateEventHandler OnCommandTerminated;

        public struct CommandExecutionJob : IJobForEach<CommandListener, SpatialEntityId>
        {
            // Problem with setting to none is it makes it run through last executed again.
            // But 
            public void Execute([ChangedFilter] ref CommandListener commandListener, [ReadOnly] ref SpatialEntityId spatialEntityId)
            {
                if (commandListener.CommandType == CommandType.None) return;
                // Only if not already running.
                switch (commandListener.CommandType)
                {
                    case CommandType.Collect:
                        OnCommandExecute?.Invoke(spatialEntityId.EntityId, typeof(CollectBehaviour), commandListener);
                        break;
                    case CommandType.Move:
                        //This will invoke OnCommandExecute passing in entity Id and the command payload.
                        OnCommandExecute?.Invoke(spatialEntityId.EntityId, typeof(MoveBehaviour), commandListener);
                        break;
                }
                commandListener.CommandType = CommandType.None;
            }
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (assignedJobHandle)
            {
                CommandExecuteJobHandle.Complete();
                assignedJobHandle = false;
            }
            CommandExecutionJob commandExecutionJob = new CommandExecutionJob();
            CommandExecuteJobHandle = commandExecutionJob.Schedule(this, inputDeps);
            assignedJobHandle = true;
            return CommandExecuteJobHandle;
        }
    }
}