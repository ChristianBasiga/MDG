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
    public class StructureMonitorSystem : JobComponentSystem
    {




        EntityQuery runningJobQuery;
        EntityQuery constructingQuery;

        Dictionary<int, byte[]> jobIdToPayload;

        System.Random randomNumberGenerator;

        WorkerSystem workerSystem;
        ComponentUpdateSystem componentUpdateSystem;
        CommandSystem commandSystem;
        struct JobEventPayloadHeader
        {
            public EntityId entityId;
            public StructureComponents.RunningJobComponent jobInfo;
        }

        struct CompleteJobPayload
        {
            public EntityId entityId;
            public int jobId;
        }

        struct ConstructionPayloadHeader
        {
            public EntityId entityId;
            public StructureComponents.BuildingComponent buildInfo;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            randomNumberGenerator = new System.Random();
            jobIdToPayload = new Dictionary<long, byte[]>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
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

            notConstructingQuery = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<StructureSchema.StructureMetadata.Component>(),
                ComponentType.Excludes<StructureComponents.BuildingComponent>()
            );
        }


        struct StartConstructingStructuresJob : IJobForEachWithEntity<NewlyAddedSpatialOSEntity, StructureSchema.StructureMetadata.Component>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            public void Execute(Entity entity, int jobIndex, [ReadOnly] NewlyAddedSpatialOSEntity newlyAddedSpatialOSEntity, [ReadOnly] StructureSchema.StructureMetadata.Component structureMetadata)
            {
                // Since just started building maybe shouldn't set build progress yet.
                entityCommandBuffer.AddComponent(index, entity, new StructureComponents.BuildingComponent
                {
                    estimatedBuildCompletion = structureMetadata.ConstructionTime,
                    buildProgress = 0
                });
            }
        }

        struct TickConstructionJob : IJobForEachWithEntity<SpatialEntityId, StructureSchema.Structure.Component,
            StructureComponents.BuildingComponent>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> structureIdToSpeed;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            // To know for which entityIds I need to send events for.
            public NativeQueue<ConstructionPayloadHeader>.ParallelWriter buildEvents;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId c0,
                ref StructureSchema.Structure.Component c2, ref StructureComponents.BuildingComponent c3)
            {
                if (structureIdToSpeed.TryGetValue(c0.EntityId, out int constructionSpeed))
                {
                    c3.buildProgress += constructionSpeed;

                    if (c3.buildProgress >= c3.estimatedBuildCompletion)
                    {
                        c2.constructing = false;
                        entityCommandBuffer.RemoveComponent<StructureComponents.BuildingComponent>(index, entity);
                    }
                    else if (!c2.constructing)
                    {
                        c2.constructing = true;
                    }
                    buildEvents.Enqueue(new ConstructionPayloadHeader
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

            public NativeQueue<CompleteJobPayload>.ParallelWriter completedJobPayloads;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId spatialEntityId, ref StructureComponents.RunningJobComponent runningJobComponent)
            {
                if (runningJobComponent.jobProgress >= runningJobComponent.estimatedJobCompletion)
                {
                    completedJobs.Enqueue(new CompleteJobPayload
                    {
                        entityId = spatialEntityId.EntityId,
                        jobId = runningJobComponent.jobId
                    });
                    entityCommandBuffer.RemoveComponent<StructureComponents.RunningJobComponent>(index, entity);
                }
                else
                {
                    float remainingTime = runningJobComponent.jobProgress + deltaTime;
                    // Bound it to estimated time incase a couple seconds off so that equality will go through properly.
                    remainingTime = Mathf.Min(remainingTime, runningJobComponent.estimatedJobCompletion);
                    // Queue to send event for.
                    toSendEventsFor.Enqueue(new JobEventPayloadHeader
                    {
                        entityId = spatialEntityId.EntityId,
                        jobInfo = runningJobComponent
                    });
                }
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float deltaTime = Time.deltaTime;
            NativeHashMap<EntityId, StructureComponents.BuildingComponent> constructingStructures = new NativeHashMap<EntityId, StructureComponents.BuildingComponent>(Allocator.TempJob);

            NativeQueue<JobEventPayloadHeader> jobsToSendEventsFor = new NativeQueue<JobEventPayloadHeader>(Allocator.TempJob);
            NativeQueue<CompleteJobPayload> completedJobPayloads = new NativeQueue<CompleteJobPayload>(Allocator.TempJob);
            TickActiveJobsJob tickActiveJobsJob = new TickActiveJobsJob
            {
                deltaTime = deltaTime,
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                toSendEventsFor = jobsToSendEventsFor.AsParallelWriter(),
                completedJobPayloads = completedJobPayloads.AsParallelWriter()
            };

            JobHandle tickJobsHandle = tickActiveJobsJob.Schedule(runningJobQuery);
            NativeQueue<ConstructionPayloadHeader> constructionsToSendEventsFor = new NativeQueue<ConstructionPayloadHeader>(Allocator.TempJob);



            // I make this last the longest but it is significantly smallet set executing on than ticking.
            // May not be able to actually run this together due to both using command buffer.
            StartConstructingStructuresJob startConstructingStructuresJob = new StartConstructingStructuresJob
            {
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };
            JobHandle startConstructionJobHandle = startConstructingStructuresJob.Schedule(notConstructingQuery);


            NativeHashMap<EntityId, int> structureIdToBuildSpeed = ProcessBuildRequests();
            TickConstructionJob tickConstructionJob = new TickConstructionJob
            {
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                buildEvents = constructionsToSendEventsFor.AsParallelWriter(),
                structureIdToSpeed = structureIdToBuildSpeed
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

            while (completedJobPayloads.Count > 0)
            {
                CompleteJobPayload jobPayload = completedJobPayloads.Dequeue();
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.JobComplete.Event(new StructureSchema.JobCompleteEventPayload
                {
                    JobData = jobIdToPayload[jobPayload.jobId],
                }), jobPayload.entityId);
            }
            completedJobPayloads.Dispose();
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

            startConstructionJobHandle.Complete();
            constructionsToSendEventsFor.Dispose();
            structureIdToBuildSpeed.Dispose();

            return inputDeps;
        }

        private NativeHashMap<EntityId, int> ProcessBuildRequests()
        {
            var buildRequests = commandSystem.GetRequestsRecieved<StructureSchema.Structure.Build.RecievedRequest>();
            NativeHashMap<EntityId, int> structureIdToBuildSpeed = new NativeHashMap<EntityId, int>(Allocator.TempJob);
            for (int i = 0; i < buildRequests.Count; ++i)
            {
                ref readonly var buildRequest = ref buildRequests[i];
                if (structureIdToBuildSpeed.TryGetValue(buildRequest.EntityId, out int buildRate))
                {
                    structureIdToBuildSpeed[buildRequest.EntityId] = buildRate + buildRequest.Payload.buildRate;
                }
                else
                {
                    structureIdToBuildSpeed.TryAdd(buildRequest.EntityId, buildRequest.Payload.buildRate);
                }

                // Need to queue these. Would require me to iterate though and they get event anyway.. Okay
                //  no payload needed in response just ack that it went through.
                commandSystem.SendResponse(new StructureSchema.Structure.Build.Response
                {
                    RequestId = buildRequest.RequestId,
                    Payload = new StructureSchema.BuildResponsePayload()
                });
            }
            return structureIdToBuildSpeed;
        }

        private void ProcessJobRequests()
        {
            var jobRequests = commandSystem.GetRequestsRecieved<StructureSchema.Structure.StartJob.RecievedRequest>();
            for (int i = 0; i < jobRequests.Count; ++i)
            {
                ref readonly var jobRequest = ref jobRequests[i];

                workerSystem.TryGetEntity(jobRequest.EntityId, out Entity structureEntity);

                int jobId = randomNumberGenerator.Next();
                jobIdToPayload[jobId] = jobRequest.Payload.JobData;
                PostUpdateCommands.AddComponent(structureEntity, new StructureComponents.RunningJobComponent
                {
                    estimatedJobCompletion = jobRequest.Payload.EstimatedJobCompletion,
                    jobProgress = 0,
                    jobId = jobId
                });
                commandSystem.SendResponse(new StructureSchema.Structure.StartJob.Response
                {
                    RequestId = jobRequest.RequestId
                });
            }
        }
    }
}