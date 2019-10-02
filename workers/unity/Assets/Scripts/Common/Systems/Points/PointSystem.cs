using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using CommonComponents = MDG.Common.Components;
using PointSchema = MdgSchema.Common.Point;

namespace MDG.Common.Systems
{
    /// <summary>
    /// More needs to be complete for this to work.
    /// I'll add all components needed to meet use cases and just add as needed though tests, but no other systems acting upon them.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)]
    public class PointSystem : ComponentSystem
    {
        NativeHashMap<EntityId, int> pointsAllocations;

        // A per frame buffer to get entities stored in above map in parallel of updating others.
        NativeHashMap<EntityId, int> allocationBuffer;
        EntityQuery initialSpawnGroup;
        EntityQueryBuilder pointGroup;
        CommandSystem commandSystem;
        int startingbuffer = 100;
        // I should reserve these.
        EntityId pointWorkerId = new EntityId(90);

        JobHandle? storePointEntityJobHandle;
        JobHandle pointAllocationJobHandle;

        // Could be stored in map, then simply sum points.
        struct PointAllocationPayload
        {
            public EntityId toAlterPointsOf;
            public int points;
        }


        struct StorePointEntityJob : IJobForEach<NewlyAddedSpatialOSEntity, SpatialEntityId, PointSchema.PointMetadata.Component>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, int>.ParallelWriter pointAllocation;

            public void Execute([ReadOnly] ref NewlyAddedSpatialOSEntity spatialOSEntity, [ReadOnly] ref SpatialEntityId spatialEntityId, 
                [ReadOnly] ref PointSchema.PointMetadata.Component pointMetadata)
            {
                pointAllocation.TryAdd(spatialEntityId.EntityId, pointMetadata.StartingPoints);
            }
        }

        struct IdlePointGainJob : IJobForEach<SpatialEntityId, PointSchema.PointMetadata.Component, PointSchema.Point.Component>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, int>.ParallelWriter pointAllocation;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref PointSchema.PointMetadata.Component pointMetadata, [ReadOnly] ref PointSchema.Point.Component pointComponent)
            {
                int updatedValue = pointComponent.Value + pointMetadata.IdleGainRate;
                pointAllocation.TryAdd(spatialEntityId.EntityId, updatedValue);
            }
        }


        struct PointAllocationJob : IJobForEach<SpatialEntityId, PointSchema.Point.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> pointAllocations;
            public void Execute(ref SpatialEntityId c0, ref PointSchema.Point.Component c1)
            {
                if (pointAllocations.TryGetValue(c0.EntityId, out int points))
                {
                    c1.Value = points;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            pointsAllocations = new NativeHashMap<EntityId, int>(startingbuffer, Allocator.Persistent);
            commandSystem = World.GetExistingSystem<CommandSystem>();
            initialSpawnGroup = GetEntityQuery(
                typeof(NewlyAddedSpatialOSEntity),
                typeof(PointSchema.PointMetadata.Component)
                );
            pointGroup = Entities.WithNone(typeof(NewlyAddedSpatialOSEntity)).WithAll(ComponentType.ReadWrite<PointSchema.Point.Component>());
        }

        protected override void OnDestroy()
        {
            if (allocationBuffer.IsCreated)
            {
                allocationBuffer.Dispose();
            }
            pointsAllocations.Dispose();

            base.OnDestroy();
        }

        // POint system sits on server. So will likely have requets.
        protected override void OnUpdate()
        {
            pointAllocationJobHandle.Complete();

            if (storePointEntityJobHandle.HasValue)
            {
                storePointEntityJobHandle.Value.Complete();
                storePointEntityJobHandle = null;

                // Combine allocation buffe into pointsAllocations.

                NativeArray<EntityId> keyArray = allocationBuffer.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < keyArray.Length; ++i)
                {
                    pointsAllocations.TryAdd(keyArray[i], allocationBuffer[keyArray[i]]);
                }
                keyArray.Dispose();
                allocationBuffer.Dispose();
            }


            int toAddCount = initialSpawnGroup.CalculateEntityCount();

            if (toAddCount > 0)
            {
                allocationBuffer = new NativeHashMap<EntityId, int>(toAddCount, Allocator.TempJob);


                StorePointEntityJob storePointEntityJob = new StorePointEntityJob
                {
                    pointAllocation = allocationBuffer.AsParallelWriter()
                };

                storePointEntityJobHandle = storePointEntityJob.Schedule(this);
            }

            int pointsCount = pointGroup.ToEntityQuery().CalculateEntityCount();

            if (pointsCount > pointsAllocations.Capacity)
            {
                pointsAllocations.Capacity = pointsCount;
            }


            IdlePointGainJob idlePointGainJob = new IdlePointGainJob
            {
                pointAllocation = pointsAllocations.AsParallelWriter()
            };

            idlePointGainJob.Schedule(this).Complete();


            // Handles point change requests that aren't handled by jobs.
            var pointRequests = commandSystem.GetRequests<PointSchema.Point.UpdatePoints.ReceivedRequest>(pointWorkerId);
            for (int i = 0; i < pointRequests.Count; ++i)
            {
                ref readonly var request = ref pointRequests[i];
                var payload = request.Payload;
                // I mean, if I run initial job, this SHOULD always be true.
                if (pointsAllocations.TryGetValue(payload.EntityUpdating, out int currentPoints))
                {
                    int updatedPoints = currentPoints + payload.PointUpdate;
                    pointsAllocations[payload.EntityUpdating] = updatedPoints;
                }
                else
                {
                    pointsAllocations.TryAdd(payload.EntityUpdating, request.Payload.PointUpdate);
                }

                commandSystem.SendResponse(new PointSchema.Point.UpdatePoints.Response
                {
                    RequestId = request.RequestId,
                    Payload = new PointSchema.PointResponse
                    {
                        TotalPoints = pointsAllocations[payload.EntityUpdating]
                    }
                }); 

            }
            PointAllocationJob pointAllocationJob = new PointAllocationJob
            {
                pointAllocations = pointsAllocations
            };
            pointAllocationJobHandle = pointAllocationJob.Schedule(this);
        }
    }
}