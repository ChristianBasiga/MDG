using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using MDG.Hunter.Components;
using MDG.Common.Components;
using Improbable.Gdk.Core;
using MDG.Hunter.Commands;
using log4net;
//really..
using UnityEngine.Jobs;
using MDG.Common.Systems;
using MdgSchema.Common;

namespace MDG.Hunter.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(MouseInputSystem))]
    public class CommandGiveSystem : JobComponentSystem
    {


        //Meta data should be only thing I add.
        public struct CommandPending
        {
            //public ICommand Command;
            public UnityEngine.Vector3 Dest;
            public EntityId TargetId;
            public CommandType CommandMetaData;
            public EntityId CommandListenerId;
        }

        private NativeArray<CommandPending> commandsPending;
        private JobHandle commandGiveJobHandle;
        public struct ClickablePayload
        {
            public EntityId clickableId;
            public GameEntityTypes gameEntityType;
        }

        private bool assignedJobHandle = false;
        JobHandle jobHandle;
        NativeArray<CommandPending> pendingCommands;
        public delegate void PendingCommandEventHandler(CommandPending commandPending);
        public event PendingCommandEventHandler OnCommandPending;

        public struct GetClickablesJob : IJobForEach<SpatialEntityId, Clickable, GameMetadata.Component>
        {
            [WriteOnly]
            public NativeArray<ClickablePayload> clickableFilter;
            public NativeArray<int> length;
            public void Execute([ReadOnly] ref  SpatialEntityId c1, [ReadOnly] ref Clickable c2, [ReadOnly] ref GameMetadata.Component c3)
            {
                //Only care if they are clicked.
                if (c2.Clicked)
                {
                    clickableFilter[length[0]++] = new ClickablePayload
                    {
                        clickableId = c1.EntityId,
                        gameEntityType = c3.Type
                    };
                }
            }
        }

        //IJobParallelForTransform jobParallelForTransforms;
        public struct CommandQueryJob : IJobForEach<CommandGiver, SpatialEntityId, MouseInputComponent>
        {
            [ReadOnly]
            public NativeArray<ClickablePayload> clickables;
            public int clickableCount;
            [WriteOnly]
            public NativeArray<CommandPending> commandsPending;
            public int commandsPendingBuffer;
            public NativeArray<int> index;
            public void Execute(ref CommandGiver commandGiver, [ReadOnly] ref SpatialEntityId cgId, [ChangedFilter] ref MouseInputComponent mouseInputComponent)
            {

                if (!mouseInputComponent.DidClickThisFrame || index[0] >= commandsPendingBuffer) return;

                EntityId commandGiverId = cgId.EntityId;
                GameEntityTypes rightClickedMeta = GameEntityTypes.Invalid;
                EntityId rightClickedId = new EntityId(-1);
                //Assume no listener.
                //Don't need to check clicked, cause the fact these are in this array is proof that clicked.
                EntityId selectedListener = new EntityId(-1);
                for (int i = 0; i < clickableCount; ++i)
                {
                    ClickablePayload clickablePayload = clickables[i];

                    if (clickablePayload.clickableId.Equals(mouseInputComponent.SelectedEntityId))
                    {
                        //If left click, mark as selected for command giver.
                        if (mouseInputComponent.LeftClick)
                        {
                           // selectedListener = clickablePayload.clickableId;
                            commandGiver.SelectedListener = clickablePayload.clickableId;
                           // HasSelected is useless when can jsut check if selectedListener valid, probably removing it.
                            commandGiver.HasSelected = true;
                        }
                        //If right clicked, and didn't right click same one, maybe functionality for that elsehwere but not for commands.
                        else if (mouseInputComponent.RightClick && !commandGiver.SelectedListener.Equals(clickablePayload.clickableId))
                        {
                            rightClickedMeta = clickablePayload.gameEntityType;
                            rightClickedId = clickablePayload.clickableId;
                        }

                    }
                }
                if (mouseInputComponent.RightClick && commandGiver.HasSelected && commandGiver.SelectedListener.IsValid())
                {
                    CommandPending commandPending;
                    CommandType commandType = CommandType.None;
                    if (!rightClickedId.IsValid())
                    {
                        commandType = CommandType.Move;
                        commandPending = new CommandPending
                        {
                            CommandMetaData = CommandType.Move,
                            Dest = mouseInputComponent.LastClickedPos
                        };
                    }
                    else
                    {

                        // Dest for these actually locaton of unit but for this frame that is same.
                        // then during execution of command get position of target id.
                        switch (rightClickedMeta)
                        {
                            case GameEntityTypes.Unit:
                                commandType = CommandType.Attack;
                                break;
                            case GameEntityTypes.Resource:
                                commandType = CommandType.Collect;
                                break;
                        }
                       
                    }
                    commandPending = new CommandPending
                    {
                        CommandMetaData = commandType,
                        Dest = mouseInputComponent.LastClickedPos,
                        TargetId = rightClickedId,
                    };
                    commandPending.CommandListenerId = commandGiver.SelectedListener;
                    commandsPending[index[0]++] = commandPending;
                    commandGiver.HasSelected = false;
                    commandGiver.SelectedListener = new EntityId(-1);
                }
            }
        }

        public struct CommandGiveJob : IJobForEach<CommandListener, SpatialEntityId>
        {
            [ReadOnly]
            public NativeArray<CommandPending> commandsPending;
            public int commandsPendingCount;
            public void Execute(ref CommandListener c0, [ReadOnly] ref SpatialEntityId c1)
            {
                for (int i = 0; i < commandsPendingCount; ++i)
                {
                    CommandPending commandPending = commandsPending[i];
                    if (commandPending.CommandMetaData != CommandType.None)
                    {
                        if (commandPending.CommandListenerId.Equals(c1.EntityId))
                        {
                            c0.CommandType = commandPending.CommandMetaData;
                            c0.TargetId = commandPending.TargetId;
                            c0.TargetPosition = commandPending.Dest;
                            break;
                        }
                    }
                }
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            if (assignedJobHandle)
            {
                //If not completed yet by next frame after all other updates, dispose and complete.
                commandsPending.Dispose();
                commandGiveJobHandle.Complete();
                assignedJobHandle = false;
            }
            //There could be alot of commandListeners, figure out those numbers later.
            //Think of better way to set information other than NativeArrays.
            //But they're only way to pass by ref since job structs don't take in ref type members.
            NativeArray<ClickablePayload> clickables = new NativeArray<ClickablePayload>(100, Allocator.TempJob);
            NativeArray<int> clickableCount = new NativeArray<int>(1, Allocator.TempJob);

            // Gotta add buffer here, then batch as needed. I feel like doing complete on this defeats purpose of it being job.
            // I could also just literally do entitymanager query here instead of doing a job since this is literally just a fetch.
            GetClickablesJob getCommandListenersJob = new GetClickablesJob
            {
                clickableFilter = clickables,
                length = clickableCount,
            };
            JobHandle commandListenersFetchHandle = getCommandListenersJob.ScheduleSingle(this, inputDeps);
            commandListenersFetchHandle.Complete();
            commandsPending = new NativeArray<CommandPending>(5, Allocator.TempJob);
            NativeArray<int> commandPendingCount = new NativeArray<int>(1, Allocator.TempJob);
            CommandQueryJob commandQueryJob = new CommandQueryJob
            {
                commandsPending = commandsPending,
                index = commandPendingCount,
                clickables = clickables,
                clickableCount = clickableCount[0],
                commandsPendingBuffer = 10
            };
            JobHandle commandQueryHandle = commandQueryJob.Schedule(this, commandListenersFetchHandle);
            commandQueryHandle.Complete();
            clickableCount.Dispose();
            clickables.Dispose();
            if (commandPendingCount[0] == 0)
            {
                commandPendingCount.Dispose();
                commandsPending.Dispose();
                return commandQueryHandle;
            }
            CommandGiveJob commandGiveJob = new CommandGiveJob
            {
                commandsPending = commandsPending,
                commandsPendingCount = commandPendingCount[0]
            };
            commandPendingCount.Dispose();
            commandGiveJobHandle = commandGiveJob.Schedule(this, commandQueryHandle);
            assignedJobHandle = true;
            return commandGiveJobHandle;
        }
    }
}