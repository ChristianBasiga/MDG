using Improbable;
using Improbable.Gdk.Core;
using MDG.Common.Components;
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

        public const float MinSelectionSize = 10;

        NativeArray<bool> didSelectThisFrame;
        JobHandle selectedJobHandle;
        EntityQuery selectorGroup;
        NativeHashMap<EntityId, SelectionBounds> idToSelectionBounds;
  

        public struct SelectionBounds
        {
            public float3 botLeft;
            public float3 topRight;
            // For clicking directly on one entity.
            public bool onlySelectOne;
        }

        // Shit, on Start running is not what I want. OnCreate Manager maybe?
        // or make sure only happens once, that's pretty fucked and would slow shit down
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            selectorGroup = GetEntityQuery(typeof(Selection));
            if (!didSelectThisFrame.IsCreated)
            didSelectThisFrame = new NativeArray<bool>(1, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (didSelectThisFrame.IsCreated)
            {
                didSelectThisFrame.Dispose();
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
                
                
                // Down line move this to set selections part.
                // reason is min size depends on entity checking selection for.
                float selectionArea = math.distance(botLeft, topRight);
                bool onlySelectOne = false;
                if (selectionArea < MinSelectionSize)
                {
                    botLeft += new float3(-5, -5, 0) * (MinSelectionSize - selectionArea) * .5f;
                    topRight += new float3(+5, +5, 0) * (MinSelectionSize - selectionArea) * .5f;
                    onlySelectOne = true;
                }
                idToSelectionBounds.TryAdd(spatialEntityId.EntityId, new SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight,
                    onlySelectOne = onlySelectOne
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

            // Really only exists to see if I'e already selected one, an array purely incase
            // more than one invader type player in game, I won't have that
            // but capabilities are there.
            [DeallocateOnJobCompletion]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<EntityId> selected;
            public int index;

            public NativeArray<bool> didSelect;
            // Instead of single, do double buffering, have a selected to read from, and one to writ eto?
            public void Execute([ReadOnly] ref SpatialEntityId id, [ReadOnly] ref EntityTransform.Component entityTransform, ref Clickable clickable)
            {
                NativeArray<EntityId> selectorIds = idToSelectionBounds.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < selectorIds.Length; ++i)
                {
                    if (idToSelectionBounds.TryGetValue(selectorIds[i], out SelectionBounds selectionBounds))
                    {
                        if (selectionBounds.onlySelectOne && selected.Contains(selectorIds[i]))
                        {
                            continue;
                        }
                        Vector3f position = entityTransform.Position;
                        // Check if position of clickable entity is within selection bounds.
                        if (position.X > selectionBounds.botLeft.x && position.Z > selectionBounds.botLeft.y 
                            && position.X < selectionBounds.topRight.x && position.Z < selectionBounds.topRight.y)
                        {
                            didSelect[0] = true;
                            clickable.Clicked = true;
                            clickable.ClickedEntityId = selectorIds[i];
                            if (selectionBounds.onlySelectOne)
                            {
                                selected[index++] = selectorIds[i];
                            }
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

            didSelectThisFrame[0] = false;
            // If selector count isn't 0, then new selection has been made this frame, reset Clickables.
            ResetSelectedEntities resetSelectedEntitiesJob = new ResetSelectedEntities();
            resetSelectedEntitiesJob.Schedule(this).Complete();
            idToSelectionBounds = new NativeHashMap<EntityId, SelectionBounds>(selectorCount, Allocator.TempJob);
            GetSelectedBounds getSelectedBounds = new GetSelectedBounds
            {
                idToSelectionBounds = idToSelectionBounds.AsParallelWriter()
            };
            JobHandle selectedBoundsJob = getSelectedBounds.Schedule(this);
            selectedBoundsJob.Complete();


            // ECS way would be for me to query clickables and check if any are currently clicked.
            // idk if faster to have event and the extra memory or nah.
            SetSelectedEntities setSelectedEntities = new SetSelectedEntities
            {
                idToSelectionBounds = idToSelectionBounds,
                selected = new NativeArray<EntityId>(selectorCount, Allocator.TempJob),
                index = 0,
                didSelect = didSelectThisFrame
            };
            selectedJobHandle = setSelectedEntities.Schedule(this, selectedBoundsJob);
            selectedJobHandle.Complete();
            OnUnitSelectionUpdated?.Invoke(didSelectThisFrame[0]);
            idToSelectionBounds.Dispose();


            RemoveSelections removeSelections = new RemoveSelections
            {
                commandBuffer = PostUpdateCommands.ToConcurrent()
            };
            removeSelections.Schedule(this).Complete();
        }
    }
}