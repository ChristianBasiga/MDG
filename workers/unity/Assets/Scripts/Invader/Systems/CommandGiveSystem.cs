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
using MDG.Common;

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
        EntityQuery rightClickablesQuery;


        private ClientGameObjectCreator clientGameObjectCreator;
        LinkedEntityComponent invaderLink;
        public LinkedEntityComponent InvaderLink 
         {
            get
            {
                if (invaderLink == null)
                {
                    invaderLink = GameObject.FindGameObjectWithTag("Player").GetComponent<LinkedEntityComponent>();
                }
                return invaderLink;
            }
        }
        BuildCommand? queuedBuildCommand;

        public struct CommandGiveJob : IJobForEachWithEntity<Clickable, MdgSchema.Units.Unit.Component, CommandListener>
        {
            public EntityId hunterId;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public CommandListener commandGiven;

            public MoveCommand? moveCommand;
            public CollectCommand? collectCommand;
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
            clientGameObjectCreator = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().ClientGameObjectCreator;
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

            rightClickablesQuery = GetEntityQuery(
                ComponentType.ReadOnly<Clickable>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<GameMetadata.Component>()
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

            CommandListener command = new CommandListener
            {
                CommandType = CommandType.None
            };

            BuildCommand? buildCommand = queuedBuildCommand;
            if (queuedBuildCommand.HasValue)
            {
                command.CommandType = CommandType.Build;
                queuedBuildCommand = null;
            }
            else
            {
                // For handling click context commands.
                if (workerUnitQuery.CalculateEntityCount() == 0 || !Input.GetMouseButtonDown(1))
                {
                    return;
                }
                command = ProcessCommandGiven();
            }

            CommandGiveJob commandGiveJob = new CommandGiveJob
            {
                commandGiven = command,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                hunterId = InvaderLink.EntityId,
                buildCommand = buildCommand
            };
            JobHandle jobHandle = commandGiveJob.Schedule(this);
            jobHandle.Complete();
        }

        // There really are SO many clickables potentially though.
        private CommandListener ProcessCommandGiven()
        {
            float3 mousePos = HelperFunctions.GetMousePosition(InvaderLink.GetComponent<Camera>());
            CommandListener commandListener = new CommandListener
            {
                CommandType = CommandType.None,
                TargetPosition = new Vector3f(mousePos.x, 20, mousePos.z)
            };
            Entities.With(rightClickablesQuery).ForEach((ref SpatialEntityId spatialEntityId, ref Clickable clickable, ref GameMetadata.Component gameMetadata) =>
            {
                if (clickable.Clicked && clickable.ClickedEntityId.Equals(InvaderLink.EntityId))
                {
                    commandListener.TargetId = spatialEntityId.EntityId;
                    switch (gameMetadata.Type)
                    {
                        case GameEntityTypes.Resource:
                            commandListener.CommandType = CommandType.Collect;
                            break;
                        case GameEntityTypes.Unit:
                            break;
                        case GameEntityTypes.Hunted:
                            commandListener.CommandType = CommandType.Attack;
                            break;
                    }
                }
            });

            if (commandListener.CommandType == CommandType.None)
            {
                commandListener.CommandType = CommandType.Move;

            }
            return commandListener;
        }
    }
}