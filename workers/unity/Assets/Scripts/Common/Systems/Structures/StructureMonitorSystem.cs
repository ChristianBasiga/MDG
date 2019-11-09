using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using StructureComponents = MDG.Common.Components.Structure;
using StructureSchema = MdgSchema.Common.Structure;
using Improbable.Gdk.Core;
using UnityEngine;
using Unity.Jobs;

namespace MDG.Common.Systems.Structure
{
    public class StructureMonitorSystem : ComponentSystem
    {
        EntityQuery runningJobQuery;
        EntityQuery constructingQuery;
        ComponentUpdateSystem componentUpdateSystem;
        struct JobEventPayloadHeader
        {
            public EntityId entityId;
            public StructureComponents.RunningJobComponent jobInfo;
        }

        struct ConstructionPayloadHeader
        {
            public EntityId entityId;
            public StructureComponents.BuildingComponent buildInfo;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            runningJobQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<StructureComponents.RunningJobComponent>()
                );
            constructingQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<StructureSchema.Structure.ComponentAuthority>(),
                ComponentType.ReadWrite<StructureSchema.Structure.Component>(),
                ComponentType.ReadWrite<StructureComponents.BuildingComponent>()
                );
            constructingQuery.SetFilter(StructureSchema.Structure.ComponentAuthority.Authoritative);
        }

        struct TickConstructionJob : IJobForEachWithEntity<SpatialEntityId, StructureSchema.Structure.Component, 
            StructureComponents.BuildingComponent>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            // To know for which entityIds I need to send events for.
            public NativeQueue<ConstructionPayloadHeader>.ParallelWriter stillBuilding;
            public float deltaTime;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId c0,  
                ref StructureSchema.Structure.Component c2, ref StructureComponents.BuildingComponent c3)
            {

                if (c3.buildProgress >= c3.estimatedBuildCompletion)
                {
                    entityCommandBuffer.RemoveComponent<StructureComponents.BuildingComponent>(index, entity);
                    // Prob remove this flag, if I will opt for event.
                    // then again if keep this flag don't gotta have event or extra queue.
                    c2.Constructing = false;
                    /*
                    completed.Enqueue(new ConstructionPayloadHeader
                    {
                        entityId = c0.EntityId,
                        buildInfo = c3
                    });*/
                }
                else
                {
                    c2.Constructing = true;
                    float remainingTime = c3.buildProgress + deltaTime;
                    // Bound it to estimated time incase a couple seconds off so that equality will go through properly.
                    remainingTime = Mathf.Min(remainingTime, c3.estimatedBuildCompletion);
                    // Queue to send event for.
                    stillBuilding.Enqueue(new ConstructionPayloadHeader
                    {
                        entityId = c0.EntityId,
                        buildInfo = c3
                    });
                }
            }
        }


        struct TickActiveJobsJob : IJobForEachWithEntity<SpatialEntityId, StructureComponents.RunningJobComponent>
        {
            public float deltaTime;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public NativeQueue<JobEventPayloadHeader>.ParallelWriter toSendEventsFor;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId c0, ref StructureComponents.RunningJobComponent c1)
            {
                if (c1.jobProgress >= c1.estimatedJobCompletion)
                {
                    entityCommandBuffer.RemoveComponent<StructureComponents.BuildingComponent>(index, entity);
                    // Prob remove this flag, if I will opt for event.
                    // then again if keep this flag don't gotta have event or extra queue.
                }
                else
                {
                    float remainingTime = c1.jobProgress + deltaTime;
                    // Bound it to estimated time incase a couple seconds off so that equality will go through properly.
                    remainingTime = Mathf.Min(remainingTime, c1.estimatedJobCompletion);
                    // Queue to send event for.
                    toSendEventsFor.Enqueue(new JobEventPayloadHeader
                    {
                        entityId = c0.EntityId,
                        jobInfo = c1
                    });
                }
            }
        }


        protected override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            NativeQueue<JobEventPayloadHeader> jobsToSendEventsFor = new NativeQueue<JobEventPayloadHeader>(Allocator.TempJob);

            TickActiveJobsJob tickActiveJobsJob = new TickActiveJobsJob
            {
                deltaTime = deltaTime,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                toSendEventsFor = jobsToSendEventsFor.AsParallelWriter()
            };

            JobHandle tickJobsHandle = tickActiveJobsJob.Schedule(runningJobQuery);
            NativeQueue<ConstructionPayloadHeader> constructionsToSendEventsFor = new NativeQueue<ConstructionPayloadHeader>(Allocator.TempJob);

            TickConstructionJob tickConstructionJob = new TickConstructionJob
            {
                deltaTime = deltaTime,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                stillBuilding = constructionsToSendEventsFor.AsParallelWriter()
            };

            JobHandle tickConstructionJobHandle = tickConstructionJob.Schedule(constructingQuery);

            tickJobsHandle.Complete();

            // Send all events for jobs progress to update UI.
            while (jobsToSendEventsFor.Count > 0)
            {
                JobEventPayloadHeader jobEventPayloadHeader = jobsToSendEventsFor.Dequeue();
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.RunJob.Event(new StructureSchema.JobRunEventPayload
                {
                    EstimatedJobCompletion = jobEventPayloadHeader.jobInfo.estimatedJobCompletion,
                    JobProgress = jobEventPayloadHeader.jobInfo.jobProgress,
                    JobType = jobEventPayloadHeader.jobInfo.jobType
                }), jobEventPayloadHeader.entityId);

            }
            jobsToSendEventsFor.Dispose();

            tickConstructionJobHandle.Complete();

            while (constructionsToSendEventsFor.Count > 0)
            {
                ConstructionPayloadHeader constructionPayloadHeader = constructionsToSendEventsFor.Dequeue();
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.Build.Event(new StructureSchema.BuildEventPayload
                {
                    BuildProgress = constructionPayloadHeader.buildInfo.buildProgress,
                    EstimatedBuildTime = constructionPayloadHeader.buildInfo.estimatedBuildCompletion
                }), constructionPayloadHeader.entityId);
            }
            constructionsToSendEventsFor.Dispose();
        }
    }
}