using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using MDG.Invader.Components;
using MDG.Common.Components;
using Improbable.Gdk.Core;
using MDG.Invader.Commands;
using log4net;
using UnityEngine.Jobs;
using MDG.Common.Systems;
using MdgSchema.Common;
using UnityEngine;
using Unity.Mathematics;
using MdgSchema.Units;
using Improbable.Gdk.Subscriptions;
using Improbable;
using MDG.Invader.Monobehaviours;

namespace MDG.Invader.Systems
{
    // For hunter command givers.
    [DisableAutoCreation]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    [UpdateAfter(typeof(SelectionSystem))]
    public class CommandGiveSystem : ComponentSystem
    {

        EntityQuery commandListenerQuery;
        EntityQuery workerUnitQuery;


        
        public LinkedEntityComponent InvaderLink { private set; get; }

        BuildCommand? queuedBuildCommand;
        // Checks what command the right click signified,
        // This would be applying the command to each Unit.
        // what I really need to do first is process it.
        public struct CommandProcessJob : IJobForEach<SpatialEntityId, Clickable, GameMetadata.Component, EntityTransform.Component>
        {
            public float3 botLeft;
            public float3 topRight;

            public NativeArray<CommandListener> commandMetadata;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Clickable clickable, [ReadOnly] ref GameMetadata.Component gameMetadata, [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                if (commandMetadata[0].CommandType != CommandType.None) return;

                if (entityTransform.Position.X > botLeft.x && entityTransform.Position.Z > botLeft.z
                    && entityTransform.Position.X < topRight.x && entityTransform.Position.Z < topRight.z)
                {
                    CommandListener command = new CommandListener {
                        TargetId = spatialEntityId.EntityId,
                        TargetPosition = entityTransform.Position
                    };
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
                    commandMetadata[0] = command;
                }
            }
        }

        // Maybe should also add worker component.
        public struct GetWorkersJob : IJobForEachWithEntity<CommandListener, MdgSchema.Units.Unit.Component, 
            SpatialEntityId, WorkerUnit>
        {
            public NativeQueue<EntityId>.ParallelWriter freeWorkers;
            public NativeQueue<EntityId>.ParallelWriter busyWorkers;

            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref CommandListener commandListener, [ReadOnly] ref Unit.Component unitComponent, 
                [ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref WorkerUnit workerUnit)
            {
                if (commandListener.CommandType == CommandType.None)
                {
                    freeWorkers.Enqueue(spatialEntityId.EntityId);
                }
                else
                {
                    busyWorkers.Enqueue(spatialEntityId.EntityId);
                }
            }
        }

        public struct CommandGiveJob : IJobForEachWithEntity<Clickable, MdgSchema.Units.Unit.Component, CommandListener>
        {
            public EntityId hunterId;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public CommandListener commandGiven;

            public BuildCommand? buildCommand;

            public void Execute(Entity entity, int index, [ReadOnly] ref Clickable clicked, [ReadOnly] ref MdgSchema.Units.Unit.Component c1, ref CommandListener commandListener)
            {
                if (clicked.Clicked && clicked.ClickedEntityId.Equals(hunterId))
                {
                    //Clean up and stop all command compnents on unit before adding a new one.
                    // Anything in queue must be released in command update system. so pass queue in job.
                    // compare the payload to new collect command to send releases accordingly.
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(MoveCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(AttackCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(CollectCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(BuildCommand));

                    // Remove it, then add interrupt, to do rest of clean up.
                    entityCommandBuffer.AddComponent(index, entity, new CommandInterrupt
                    {
                        interrupting = commandListener.CommandType,
                        target = commandGiven.TargetId
                    });
                    commandListener.CommandType = commandGiven.CommandType;
                    commandListener.TargetPosition = commandGiven.TargetPosition;
                    commandListener.TargetId = commandGiven.TargetId;

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
                        case CommandType.Build:
                            Debug.Log("building");
                            entityCommandBuffer.AddComponent(index, entity, buildCommand.Value);
                            break;
                    }
                }
            }
        }

        // Build Command is a broadcast to available units.
        public struct GiveBuildCommandJob : IJobForEachWithEntity<SpatialEntityId, Clickable, MdgSchema.Units.Unit.Component, 
            CommandListener, WorkerUnit>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public BuildCommand buildCommand;

            [ReadOnly]
            public NativeArray<EntityId> toAssignTo;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Clickable clicked,
                [ReadOnly] ref MdgSchema.Units.Unit.Component c1, ref CommandListener commandListener,
                [ReadOnly]  ref WorkerUnit workerUnit)
            {
                if (toAssignTo.Contains(spatialEntityId.EntityId))
                {
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(MoveCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(AttackCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(CollectCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(BuildCommand));
                    entityCommandBuffer.AddComponent(index, entity, buildCommand);
                    commandListener.CommandType = CommandType.Build;
                }
            }
        }

    
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            InvaderLink = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<LinkedEntityComponent>();
            commandListenerQuery = GetEntityQuery(
              ComponentType.ReadWrite<CommandListener>(),
              ComponentType.ReadOnly<Clickable>(),
              ComponentType.ReadOnly<MdgSchema.Units.Unit.Component>()
              );
            workerUnitQuery = GetEntityQuery(
                  ComponentType.ReadOnly<CommandListener>(),
                ComponentType.ReadOnly<Clickable>(),
                ComponentType.ReadOnly<MdgSchema.Units.Unit.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<WorkerUnit>()
                );

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


        public void GiveBuildCommand(BuildCommand command)
        {
            queuedBuildCommand = command;
        }

        protected override void OnUpdate()
        {

            // Handling queued commands.

            NativeArray<CommandListener> commandGiven = new NativeArray<CommandListener>(1, Allocator.TempJob);
            commandGiven[0] = new CommandListener
            {
                CommandType = CommandType.None
            };
            BuildCommand? buildCommand = queuedBuildCommand;

            if (queuedBuildCommand.HasValue)
            {
                commandGiven[0] = new CommandListener
                {
                    CommandType = CommandType.Build
                };
                queuedBuildCommand = null;
            }
            else
            {

                // For handling click context commands.
                if (workerUnitQuery.CalculateEntityCount() == 0 || !Input.GetMouseButtonDown(1))
                {
                    commandGiven.Dispose();
                    return;
                }

                float3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                // Creates bounding box for right click big enough to sense the click.
                // Maybe create alot of these / include botLeft and botRight in the NativeArray.
                float3 botLeft = mousePos + new float3(-10, 0, -10);// * (10 - Input.mousePosition.magnitude) * .5f;
                float3 topRight = mousePos + new float3(+10, 0, +10);// * (10 - Input.mousePosition.magnitude) * .5f;

                CommandProcessJob commandProcessJob = new CommandProcessJob
                {
                    commandMetadata = commandGiven,
                    botLeft = botLeft,
                    topRight = topRight
                };
                JobHandle commandProcessHandle = commandProcessJob.ScheduleSingle(this);
                //getWorkersJobHandle.Complete();
                commandProcessHandle.Complete();


                // If right click did not overlap with any clickable and build command not queued, it is move command.
                if (commandGiven[0].CommandType == CommandType.None)
                {
                    Vector3f convertedMousePos = new Vector3f(mousePos.x, 0, mousePos.z);
                    commandGiven[0] = new CommandListener
                    {
                        TargetPosition = convertedMousePos,
                        CommandType = CommandType.Move
                    };
                }
            }

            CommandListener commandMetadata = commandGiven[0];
            CommandGiveJob commandGiveJob = new CommandGiveJob
            {
                commandGiven = commandMetadata,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                hunterId = InvaderLink.EntityId,
                buildCommand = buildCommand
            };
            JobHandle jobHandle = commandGiveJob.Schedule(this);
            commandGiven.Dispose();
            jobHandle.Complete();

        }
    }
}