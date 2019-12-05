using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common.Components;
using MDG.Common.MonoBehaviours;
using MDG.Invader.Components;
using MdgSchema.Common;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MDG.Invader.Systems {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    [UpdateBefore(typeof(CommandGiveSystem))]
    public class SelectionSystem : ComponentSystem
    {

        public event Action<bool> OnUnitSelectionUpdated;

        JobHandle selectedJobHandle;
        EntityQuery selectorGroup;
        NativeHashMap<EntityId, SelectionBounds> idToSelectionBounds;

        NativeQueue<EntityId> selectedThisFrameFromJob;


        ClientGameObjectCreator clientGameObjectCreator;
        LinkedEntityComponent invaderLink;

        public struct SelectionBounds
        {
            public float3 botLeft;
            public float3 topRight;
        }


        // Prob just use init instead of onstart running
        public void Init(ClientGameObjectCreator clientGameObjectCreator)
        {
            this.clientGameObjectCreator = clientGameObjectCreator;
            // Access from somewhere
            invaderLink = UnityEngine.Camera.main.GetComponent<LinkedEntityComponent>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            selectorGroup = GetEntityQuery(typeof(Selection));

            if (!selectedThisFrameFromJob.IsCreated)
            {
                selectedThisFrameFromJob = new NativeQueue<EntityId>(Allocator.Persistent);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (selectedThisFrameFromJob.IsCreated)
            {
                selectedThisFrameFromJob.Dispose();
            }
        }


        public struct GetSelectedBounds : IJobForEach<SpatialEntityId, Selection>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, SelectionBounds>.ParallelWriter idToSelectionBounds;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Selection selection)
            {
                // Gotta apply rotation here.
                float3 botLeft = new float3(math.min(selection.StartPosition.x, selection.EndPosition.x), math.min(selection.StartPosition.z, selection.EndPosition.z), 0);
                float3 topRight = new float3(math.max(selection.StartPosition.x, selection.EndPosition.x), math.max(selection.StartPosition.z, selection.EndPosition.z), 0);
                
                float selectionArea = math.distance(botLeft, topRight);
                idToSelectionBounds.TryAdd(spatialEntityId.EntityId, new SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight,
                });
            }
        }

        public struct RemoveSelections : IJobForEachWithEntity<Selection>
        {
            public EntityCommandBuffer.Concurrent commandBuffer;
            public void Execute(Entity entity, int index, ref Selection c0)
            {
                commandBuffer.RemoveComponent(index, entity, typeof(Selection));
            }
        }

        public struct ResetSelectedEntities : IJobForEach<Clickable>
        {
            public void Execute(ref Clickable c0)
            {
                c0.Clicked = false;
                c0.ClickedEntityId = new EntityId(-1);
            }
        }

        public struct SetSelectedEntities : IJobForEach<SpatialEntityId, EntityTransform.Component, Clickable>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, SelectionBounds> idToSelectionBounds;

            public NativeQueue<EntityId>.ParallelWriter selectedThisFrame;
            // Instead of single, do double buffering, have a selected to read from, and one to writ eto?
            public void Execute([ReadOnly] ref SpatialEntityId id, [ReadOnly] ref EntityTransform.Component entityTransform, ref Clickable clickable)
            {
                NativeArray<EntityId> selectorIds = idToSelectionBounds.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < selectorIds.Length; ++i)
                {
                    if (idToSelectionBounds.TryGetValue(selectorIds[i], out SelectionBounds selectionBounds))
                    {
                        Vector3f position = entityTransform.Position;
                        // Check if position of clickable entity is within selection bounds.
                        if (position.X > selectionBounds.botLeft.x && position.Z > selectionBounds.botLeft.y 
                            && position.X < selectionBounds.topRight.x && position.Z < selectionBounds.topRight.y)
                        {
                            clickable.Clicked = true;
                            clickable.ClickedEntityId = selectorIds[i];
                            selectedThisFrame.Enqueue(id.EntityId);
                        }
                    }
                }
                selectorIds.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            int selectorCount = selectorGroup.CalculateEntityCount();
            if (selectorCount == 0)
            {
                return;
            }
            idToSelectionBounds = new NativeHashMap<EntityId, SelectionBounds>(selectorCount, Allocator.TempJob);
            GetSelectedBounds getSelectedBounds = new GetSelectedBounds
            {
                idToSelectionBounds = idToSelectionBounds.AsParallelWriter()
            };
            JobHandle selectedBoundsJob = getSelectedBounds.Schedule(this);


            // If selector count isn't 0, then new selection has been made this frame, reset Clickables.
            ResetSelectedEntities resetSelectedEntitiesJob = new ResetSelectedEntities();
            selectedThisFrameFromJob.Clear();

            resetSelectedEntitiesJob.Schedule(this).Complete();


            Entities.ForEach((ref SpatialEntityId spatialEntityId, ref Clickable clickable) =>
            {
                ClickableMonobehaviour clickableMonobehaviour = clientGameObjectCreator.GetLinkedGameObjectById(spatialEntityId.EntityId).GetComponent<ClickableMonobehaviour>();
                if (clickableMonobehaviour.SelectedThisFrame)
                {
                    clickable.Clicked = true;
                    clickable.ClickedEntityId = invaderLink.EntityId;
                    selectedThisFrameFromJob.Enqueue(spatialEntityId.EntityId);
                }
            });
            selectedBoundsJob.Complete();

            // ECS way would be for me to query clickables and check if any are currently clicked.
            // idk if faster to have event and the extra memory or nah.
            SetSelectedEntities setSelectedEntities = new SetSelectedEntities
            {
                idToSelectionBounds = idToSelectionBounds,
                selectedThisFrame = selectedThisFrameFromJob.AsParallelWriter()
            };
            selectedJobHandle = setSelectedEntities.Schedule(this, selectedBoundsJob);
            selectedJobHandle.Complete();
            OnUnitSelectionUpdated?.Invoke(selectedThisFrameFromJob.Count > 0);
            idToSelectionBounds.Dispose();

            RemoveSelections removeSelections = new RemoveSelections
            {
                commandBuffer = PostUpdateCommands.ToConcurrent()
            };
            removeSelections.Schedule(this).Complete();
        }
    }
}