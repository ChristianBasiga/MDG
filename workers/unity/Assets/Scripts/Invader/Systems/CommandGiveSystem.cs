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

namespace MDG.Invader.Systems
{
    // For hunter command givers.
    [DisableAutoCreation]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    [UpdateAfter(typeof(SelectionSystem))]
    public class CommandGiveSystem : ComponentSystem
    {

        private ResourceRequestSystem resourceRequestSystem;

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
                    CommandListener command = new CommandListener { TargetId = spatialEntityId.EntityId, TargetPosition = entityTransform.Position };
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
            public CommandListener commandGiven;
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
                    }
                }
            }
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            resourceRequestSystem = World.GetExistingSystem<ResourceRequestSystem>();
        }

        protected override void OnUpdate()
        {
            GameObject hunter = GameObject.FindGameObjectWithTag("MainCamera");
            if (!hunter) return;
            LinkedEntityComponent linkedEntityComponent = hunter.GetComponent<LinkedEntityComponent>();
            if (linkedEntityComponent == null || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            Camera inputCamera = hunter.transform.GetChild(0).GetComponent<Camera>();
            EntityId hunterId = linkedEntityComponent.EntityId;
            float3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Debug.Log("mouse pos " + mousePos);
            // Creates bounding box for right click big enough to sense the click.
            // Maybe create alot of these / include botLeft and botRight in the NativeArray.
            float3 botLeft = mousePos + new float3(-10, 0, -10);// * (10 - Input.mousePosition.magnitude) * .5f;
            float3 topRight = mousePos + new float3(+10, 0, +10);// * (10 - Input.mousePosition.magnitude) * .5f;

            NativeArray<CommandListener> commandGiven = new NativeArray<CommandListener>(1, Allocator.TempJob);
            commandGiven[0] = new CommandListener { CommandType = CommandType.None };
            CommandProcessJob commandProcessJob = new CommandProcessJob
            {
                commandMetadata = commandGiven,
                botLeft = botLeft,
                topRight = topRight
            };
            commandProcessJob.ScheduleSingle(this).Complete();
            CommandListener commandMetadata = commandGiven[0];
            // If right click did not overlap with any clickable, then it is a move command.
            if (commandMetadata.CommandType == CommandType.None)
            {
                Vector3f convertedMousePos = new Vector3f(mousePos.x, 0, mousePos.z);
                commandMetadata = new CommandListener { TargetPosition = convertedMousePos, CommandType = CommandType.Move };
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