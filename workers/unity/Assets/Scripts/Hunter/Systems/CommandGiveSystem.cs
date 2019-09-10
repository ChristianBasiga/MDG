using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using MDG.Hunter.Components;
using MDG.Common.Components;
using Improbable.Gdk.Core;
using MDG.Hunter.Commands;
using log4net;
using UnityEngine.Jobs;
using MDG.Common.Systems;
using MdgSchema.Common;
using UnityEngine;
using Unity.Mathematics;
using MdgSchema.Units;

namespace MDG.Hunter.Systems
{
    // For hunter command givers.
    [DisableAutoCreation]
    [UpdateAfter(typeof(SelectionSystem))]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    public class CommandGiveSystem : JobComponentSystem
    {

        // Set by controller
        public EntityId HunterId { set; get; }
        EndSimulationEntityCommandBufferSystem simulationEntityCommandBufferSystem;
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
        // Checks what command the right click signified,
        // This would be applying the command to each Unit.
        // what I really need to do first is process it.
        public struct CommandProcessJob : IJobForEach<SpatialEntityId, Clickable, GameMetadata.Component, EntityTransform.Component>
        {
            public float3 botLeft;
            public float3 topRight;
            public NativeArray<bool> foundSelection;
            public NativeArray<CommandMetadata> commandMetadata;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Clickable clickable, [ReadOnly] ref GameMetadata.Component gameMetadata, [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                if (foundSelection[0]) return;

                if (entityTransform.Position.X > botLeft.x && entityTransform.Position.Z > botLeft.y
                    && entityTransform.Position.X < topRight.x && entityTransform.Position.Z < topRight.y)
                {
                    foundSelection[0] = true;
                    CommandMetadata command = new CommandMetadata { TargetId = spatialEntityId.EntityId, TargetPosition = entityTransform.Position.ToUnityVector() };
                    switch (gameMetadata.Type)
                    {
                        case GameEntityTypes.Resource:
                            command.CommandType = CommandType.Collect;
                            break;
                        case GameEntityTypes.Unit:
                            // Don't know if attack unless has enemy component.
                            // but I cannot check that here, so has to be after this job.
                            // actually in final version there are no enemy units, just enemy structures.
                            break;
                        case GameEntityTypes.Hunted:
                            command.CommandType = CommandType.Attack;
                            break;
                    }
                }
            }
        }

        /*
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
        }*/

        public struct CommandGiveJob : IJobForEachWithEntity<Clickable, MdgSchema.Units.Unit.Component, CommandListener>
        {
            public EntityId hunterId;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public CommandMetadata commandGiven;
            public void Execute(Entity entity, int index, [ReadOnly] ref Clickable clicked, [ReadOnly] ref MdgSchema.Units.Unit.Component c1, ref CommandListener commandListener)
            {
                if (clicked.Clicked && clicked.ClickedEntityId.Equals(hunterId))
                {
                    switch (commandGiven.CommandType)
                    {
                        case CommandType.Move:
                            entityCommandBuffer.AddComponent(index, entity, new MoveCommand { destination = commandGiven.TargetPosition});    
                            break;
                        case CommandType.Attack:
                            entityCommandBuffer.AddComponent(index, entity, new AttackCommand { target = commandGiven.TargetId });
                            break;
                        case CommandType.Collect:
                            entityCommandBuffer.AddComponent(index, entity, new CollectCommand { destination = commandGiven.TargetPosition, resourceId = commandGiven.TargetId });
                            break;
                    }
                }
            }
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            simulationEntityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!Input.GetMouseButtonDown(1))
            {
                return inputDeps;
            }

            float3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Creates bounding box for right click big enough to sense the click.
            float3 botLeft = mousePos + new float3(-10, -10, 0) * (10 - Input.mousePosition.magnitude) * .5f;
            float3 topRight = mousePos + new float3(+10, +10, 0) * (10 - Input.mousePosition.magnitude) * .5f;
            NativeArray<bool> foundSelection = new NativeArray<bool>(1, Allocator.TempJob);
            NativeArray<CommandMetadata> commandGiven = new NativeArray<CommandMetadata>(1, Allocator.TempJob);
            foundSelection[0] = false;
            CommandProcessJob commandProcessJob = new CommandProcessJob
            {
                foundSelection = foundSelection,
                commandMetadata = commandGiven,
                botLeft = botLeft,
                topRight = topRight
            };
            inputDeps = commandProcessJob.Schedule(this, inputDeps);
            inputDeps.Complete();
            CommandMetadata commandMetadata = commandGiven[0];
            commandGiven.Dispose();
            // If right click did not overlap with any clickable, then it is a move command.
            if (!foundSelection[0])
            {
                commandMetadata = new CommandMetadata { TargetPosition = mousePos, CommandType = CommandType.Move };
            }
            // Now command give job will add the correct command to each clickable that has been selected by entity.
            CommandGiveJob commandGiveJob = new CommandGiveJob
            {
                commandGiven = commandMetadata,
                entityCommandBuffer = simulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                hunterId = HunterId
            };
            inputDeps = commandGiveJob.Schedule(this, inputDeps);
            inputDeps.Complete();
            return inputDeps;
        }
    }
}