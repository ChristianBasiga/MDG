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
using Improbable.Gdk.Subscriptions;

namespace MDG.Hunter.Systems
{
    // For hunter command givers.
    [DisableAutoCreation]
    [UpdateAfter(typeof(SelectionSystem))]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    public class CommandGiveSystem : ComponentSystem
    {


        // Checks what command the right click signified,
        // This would be applying the command to each Unit.
        // what I really need to do first is process it.
        public struct CommandProcessJob : IJobForEach<SpatialEntityId, Clickable, GameMetadata.Component, EntityTransform.Component>
        {
            public float3 botLeft;
            public float3 topRight;

            public NativeArray<CommandMetadata> commandMetadata;

            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Clickable clickable, [ReadOnly] ref GameMetadata.Component gameMetadata, [ReadOnly] ref EntityTransform.Component entityTransform)
            {
                if (commandMetadata[0].CommandType != CommandType.None) return;

                if (entityTransform.Position.X > botLeft.x && entityTransform.Position.Z > botLeft.y
                    && entityTransform.Position.X < topRight.x && entityTransform.Position.Z < topRight.y)
                {
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
                    commandMetadata[0] = command;
                }
            }
        }

        public struct CommandGiveJob : IJobForEachWithEntity<Clickable, MdgSchema.Units.Unit.Component, CommandListener>
        {
            public EntityId hunterId;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public CommandMetadata commandGiven;
            public void Execute(Entity entity, int index, [ReadOnly] ref Clickable clicked, [ReadOnly] ref MdgSchema.Units.Unit.Component c1, ref CommandListener commandListener)
            {
                if (clicked.Clicked && clicked.ClickedEntityId.Equals(hunterId))
                {
                    //Clean up and stop all command compnents on unit before adding a new one.
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(MoveCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(AttackCommand));
                    entityCommandBuffer.RemoveComponent(index, entity, typeof(CollectCommand));
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
        }

        protected override void OnUpdate()
        {
            GameObject hunter = GameObject.FindGameObjectWithTag("Invader");
            if (hunter == null) return;
            LinkedEntityComponent linkedEntityComponent = hunter.GetComponent<LinkedEntityComponent>();
            if (linkedEntityComponent == null || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            Camera inputCamera = hunter.transform.GetChild(0).GetComponent<Camera>();
            EntityId hunterId = linkedEntityComponent.EntityId;
            float3 mousePos = inputCamera.ScreenToWorldPoint(Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);
            Debug.Log(Input.mousePosition);
            Debug.Log(mousePos);
            // Maybe it's at this point I want to appl
            float3 botLeft = mousePos + new float3(-10, -10, 0) * (10 - Input.mousePosition.magnitude) * .5f;
            float3 topRight = mousePos + new float3(+10, +10, 0) * (10 - Input.mousePosition.magnitude) * .5f;
            NativeArray<CommandMetadata> commandGiven = new NativeArray<CommandMetadata>(1, Allocator.TempJob);
            commandGiven[0] = new CommandMetadata { CommandType = CommandType.None };
            CommandProcessJob commandProcessJob = new CommandProcessJob
            {
                commandMetadata = commandGiven,
                botLeft = botLeft,
                topRight = topRight
            };
            commandProcessJob.ScheduleSingle(this).Complete();
            CommandMetadata commandMetadata = commandGiven[0];
            // If right click did not overlap with any clickable, then it is a move command.
            if (commandMetadata.CommandType == CommandType.None)
            {
                commandMetadata = new CommandMetadata { TargetPosition = mousePos, CommandType = CommandType.Move };
            }
            CommandGiveJob commandGiveJob = new CommandGiveJob
            {
                commandGiven = commandMetadata,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                hunterId = hunterId
            };
            commandGiveJob.Schedule(this).Complete();
            commandGiven.Dispose();
            //inputCamera.depth = 0;
        }
    }
}