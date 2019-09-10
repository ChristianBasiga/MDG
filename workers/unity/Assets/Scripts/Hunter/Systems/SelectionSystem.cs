using Improbable;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using MDG.Hunter.Components;
using MdgSchema.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MDG.Hunter.Systems {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    [UpdateBefore(typeof(CommandGiveSystem))]
    public class SelectionSystem : JobComponentSystem
    {
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

        protected override void OnCreate()
        {
            base.OnCreate();
            selectorGroup = GetEntityQuery(typeof(Selection));
            
        }
        public struct GetSelectedBounds : IJobForEach<SpatialEntityId, Selection>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, SelectionBounds>.ParallelWriter idToSelectionBounds;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Selection selection)
            {
                float3 botLeft = new float3(math.min(selection.StartPosition.x, selection.EndPosition.x), math.min(selection.StartPosition.z, selection.EndPosition.z), 0);
                float3 topRight = new float3(math.max(selection.StartPosition.x, selection.EndPosition.x), math.max(selection.StartPosition.z, selection.EndPosition.z), 0);

                // Down line move this to set selections part.
                // reason is min size depends on entity checking selection for.
                float selectionAreaMinSize = 10;
                float selectionArea = math.distance(botLeft, topRight);
                bool onlySelectOne = false;
                if (selectionArea < selectionAreaMinSize)
                {
                    botLeft += new float3(-5, -5, 0) * (selectionAreaMinSize - selectionArea) * .5f;
                    topRight += new float3(+5, +5, 0) * (selectionAreaMinSize - selectionArea) * .5f;
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
            [DeallocateOnJobCompletion]
            public NativeArray<EntityId> selected;

            public int index;

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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Complete previous selection setting if didn't complete last frame.
            // this may the case if selected many units.
          /*  selectedJobHandle.Complete();
            if (idToSelectionBounds.IsCreated)
            {
            }*/
            int selectorCount = selectorGroup.CalculateEntityCount();
            if (selectorCount == 0) return inputDeps;
            
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

            NativeArray<EntityId> selectedFlags = new NativeArray<EntityId>(selectorCount, Allocator.TempJob);
            SetSelectedEntities setSelectedEntities = new SetSelectedEntities
            {
                idToSelectionBounds = idToSelectionBounds,
                selected = selectedFlags,
                index = 0
            };

            selectedJobHandle = setSelectedEntities.Schedule(this, selectedBoundsJob);
            selectedJobHandle.Complete();
            idToSelectionBounds.Dispose();
            return selectedJobHandle;
        }
    }
}