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
    [AlwaysUpdateSystem]
    public class SelectionSystem : JobComponentSystem
    {
        JobHandle selectedJobHandle;
        EntityQuery selectorGroup;
        NativeHashMap<EntityId, SelectionBounds> idToSelectionBounds;   

        public struct SelectionBounds
        {
            public float3 botLeft;
            public float3 topRight;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            selectorGroup = GetEntityQuery(typeof(Selection));
            
        }

        public struct GetSelectedBounds : IJobForEachWithEntity<SpatialEntityId, Selection>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, SelectionBounds>.ParallelWriter idToSelectionBounds;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Selection selection)
            {
                float3 botLeft = new float3(math.min(selection.StartPosition.x, selection.EndPosition.x), math.min(selection.StartPosition.z, selection.EndPosition.z), 0);
                float3 topRight = new float3(math.max(selection.StartPosition.x, selection.EndPosition.x), math.max(selection.StartPosition.z, selection.EndPosition.z), 0);
                idToSelectionBounds.TryAdd(spatialEntityId.EntityId, new SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight
                });
            }
        }

        public struct SetSelectedEntities : IJobForEach<SpatialEntityId, EntityTransform.Component, Clickable>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, SelectionBounds> idToSelectionBounds;
            public void Execute([ReadOnly] ref SpatialEntityId id, [ReadOnly] ref EntityTransform.Component entityTransform, ref Clickable c0)
            {
                NativeArray<EntityId> selectorIds = idToSelectionBounds.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < selectorIds.Length; ++i)
                {
                    if (idToSelectionBounds.TryGetValue(selectorIds[i], out SelectionBounds selectionBounds))
                    {
                        Vector3f position = entityTransform.Position;

                        UnityEngine.Debug.LogError(selectionBounds.botLeft);
                        UnityEngine.Debug.LogError(selectionBounds.topRight);
                        UnityEngine.Debug.LogError(position);
                        // Check if position of clickable entity is within selection bounds.
                        if (position.X > selectionBounds.botLeft.x && position.Z > selectionBounds.botLeft.y 
                            && position.X < selectionBounds.topRight.x && position.Z < selectionBounds.topRight.y)
                        {
                            UnityEngine.Debug.LogError($" selected {id.EntityId}");
                        }
                    }

                }
            }
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            selectedJobHandle.Complete();
            if (idToSelectionBounds.IsCreated)
            {
                idToSelectionBounds.Dispose();
            }
            int selectorCount = selectorGroup.CalculateEntityCount();
            if (selectorCount == 0) return inputDeps;

            idToSelectionBounds = new NativeHashMap<EntityId, SelectionBounds>(selectorCount, Allocator.TempJob);

            GetSelectedBounds getSelectedBounds = new GetSelectedBounds
            {
                idToSelectionBounds = idToSelectionBounds.AsParallelWriter()
            };

            JobHandle selectedBoundsJob = getSelectedBounds.Schedule(this);
            selectedBoundsJob.Complete();

            SetSelectedEntities setSelectedEntities = new SetSelectedEntities
            {
                idToSelectionBounds = idToSelectionBounds
            };

            selectedJobHandle = setSelectedEntities.Schedule(this, selectedBoundsJob);

            return selectedJobHandle;
        }
    }
}