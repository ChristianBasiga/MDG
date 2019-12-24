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
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [AlwaysUpdateSystem]
    public class StructureMonitorSystem : ComponentSystem
    {
        EntityQuery runningJobQuery;
        EntityQuery constructingQuery;
        EntityQuery notConstructingQuery;

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
            jobIdToPayload = new Dictionary<int, byte[]>();
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
                ComponentType.ReadOnly<StructureSchema.Structure.Component>(),
                ComponentType.Exclude<StructureComponents.BuildingComponent>()
            );
        }

        struct StartConstructingStructuresJob : IJobForEachWithEntity<NewlyAddedSpatialOSEntity, StructureSchema.Structure.Component, StructureSchema.StructureMetadata.Component>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public void Execute(Entity entity, int jobIndex, [ReadOnly] ref NewlyAddedSpatialOSEntity newlyAddedSpatialOSEntity, 
                [ReadOnly] ref StructureSchema.Structure.Component structureComponent, [ReadOnly] ref StructureSchema.StructureMetadata.Component structureMetadata)
            {
                if (structureComponent.Constructing)
                {
                    entityCommandBuffer.AddComponent(jobIndex, entity, new StructureComponents.BuildingComponent
                    {
                        EstimatedBuildCompletion = structureMetadata.ConstructionTime,
                        BuildProgress = 0
                    });
                }
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
                    c3.BuildProgress += constructionSpeed;
                    Debug.Log($"Build progress {c3.BuildProgress}");
                    if (c3.BuildProgress >= c3.EstimatedBuildCompletion)
                    {
                        Debug.Log("BuildCompleted");
                        c2.Constructing = false;
                        entityCommandBuffer.RemoveComponent<StructureComponents.BuildingComponent>(index, entity);
                    }
                    else if (!c2.Constructing)
                    {
                        c2.Constructing = true;
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
            public NativeQueue<JobEventPayloadHeader>.ParallelWriter toSendEventsFor;

            public NativeQueue<CompleteJobPayload>.ParallelWriter completedJobPayloads;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId spatialEntityId, ref StructureComponents.RunningJobComponent runningJobComponent)
            {

                if (runningJobComponent.JobProgress >= runningJobComponent.EstimatedJobCompletion)
                {
                    completedJobPayloads.Enqueue(new CompleteJobPayload
                    {
                        entityId = spatialEntityId.EntityId,
                        jobId = runningJobComponent.JobId
                    });
                }
                else
                {
                    float remainingTime = runningJobComponent.JobProgress + deltaTime;
                    // Bound it to estimated time incase a couple seconds off so that equality will go through properly.
                    remainingTime = Mathf.Min(remainingTime, runningJobComponent.EstimatedJobCompletion);
                    runningJobComponent.JobProgress = remainingTime;
                    // Queue to send event for.
                    toSendEventsFor.Enqueue(new JobEventPayloadHeader
                    {
                        entityId = spatialEntityId.EntityId,
                        jobInfo = runningJobComponent
                    });
                }
            }
        }


        protected override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;



            NativeQueue<JobEventPayloadHeader> jobsToSendEventsFor = new NativeQueue<JobEventPayloadHeader>(Allocator.TempJob);
            NativeQueue<CompleteJobPayload> completedJobPayloads = new NativeQueue<CompleteJobPayload>(Allocator.TempJob);


            #region Scheduling Jobs
            // These can't write to postupdate buffer in parralel unless job indices are unique among each other.
            TickActiveJobsJob tickActiveJobsJob = new TickActiveJobsJob
            {
                deltaTime = deltaTime,
                toSendEventsFor = jobsToSendEventsFor.AsParallelWriter(),
                completedJobPayloads = completedJobPayloads.AsParallelWriter()
            };
            JobHandle tickJobsHandle = tickActiveJobsJob.Schedule(runningJobQuery);
            ProcessJobRequests();

            NativeQueue<ConstructionPayloadHeader> constructionsToSendEventsFor = new NativeQueue<ConstructionPayloadHeader>(Allocator.TempJob);

            // I make this last the longest but it is significantly smallet set executing on than ticking.
            // May not be able to actually run this together due to both using command buffer.
            StartConstructingStructuresJob startConstructingStructuresJob = new StartConstructingStructuresJob
            {
                entityCommandBuffer = PostUpdateCommands.ToConcurrent()
            };


            JobHandle startConstructionJobHandle = startConstructingStructuresJob.Schedule(notConstructingQuery);
            tickJobsHandle.Complete();

            startConstructionJobHandle.Complete();
            NativeHashMap<EntityId, int> structureIdToBuildSpeed = ProcessBuildRequests();
            TickConstructionJob tickConstructionJob = new TickConstructionJob
            {
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                buildEvents = constructionsToSendEventsFor.AsParallelWriter(),
                structureIdToSpeed = structureIdToBuildSpeed
            };

            JobHandle tickConstructionJobHandle = tickConstructionJob.Schedule(constructingQuery);
            #endregion
            #region Sending Events to Clients
            // Send all events for jobs progress to update UI.
            while (jobsToSendEventsFor.Count > 0)
            {
                JobEventPayloadHeader jobEventPayloadHeader = jobsToSendEventsFor.Dequeue();
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.JobRunning.Event(new StructureSchema.JobRunEventPayload
                {
                    EstimatedJobCompletion = jobEventPayloadHeader.jobInfo.EstimatedJobCompletion,
                    JobProgress = jobEventPayloadHeader.jobInfo.JobProgress,
                }), jobEventPayloadHeader.entityId);

            }
            jobsToSendEventsFor.Dispose();
            tickConstructionJobHandle.Complete();

            while (completedJobPayloads.Count > 0)
            {
                CompleteJobPayload jobPayload = completedJobPayloads.Dequeue();
                workerSystem.TryGetEntity(jobPayload.entityId, out Entity entity);
                PostUpdateCommands.RemoveComponent<StructureComponents.RunningJobComponent>(entity);
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.JobComplete.Event(new StructureSchema.JobCompleteEventPayload
                {
                    JobData = jobIdToPayload[jobPayload.jobId],
                }), jobPayload.entityId);
            }
            completedJobPayloads.Dispose();
            while (constructionsToSendEventsFor.Count > 0)
            {
                ConstructionPayloadHeader constructionPayloadHeader = constructionsToSendEventsFor.Dequeue();
                componentUpdateSystem.SendEvent(new StructureSchema.Structure.Building.Event(new StructureSchema.BuildEventPayload
                {
                    BuildProgress = constructionPayloadHeader.buildInfo.BuildProgress,
                    EstimatedBuildCompletion = constructionPayloadHeader.buildInfo.EstimatedBuildCompletion
                }), constructionPayloadHeader.entityId);
            }
            #endregion
            constructionsToSendEventsFor.Dispose();
            structureIdToBuildSpeed.Dispose();
        }

        #region Processing Structure Requests

        private NativeHashMap<EntityId, int> ProcessBuildRequests()
        {
            var buildRequests = commandSystem.GetRequests<StructureSchema.Structure.Build.ReceivedRequest>();
            NativeHashMap<EntityId, int> structureIdToBuildSpeed = new NativeHashMap<EntityId, int>( constructingQuery.CalculateEntityCount(),Allocator.TempJob);
            for (int i = 0; i < buildRequests.Count; ++i)
            {
                Debug.Log("recieved build request");
                ref readonly var buildRequest = ref buildRequests[i];
                if (structureIdToBuildSpeed.TryGetValue(buildRequest.EntityId, out int buildRate))
                {
                    structureIdToBuildSpeed[buildRequest.EntityId] = buildRate + buildRequest.Payload.BuildRate;
                }
                else
                {
                    structureIdToBuildSpeed.TryAdd(buildRequest.EntityId, buildRequest.Payload.BuildRate);
                }

                Debug.Log("sending response");
                commandSystem.SendResponse(new StructureSchema.Structure.Build.Response
                {
                    RequestId = buildRequest.RequestId,
                    Payload = new StructureSchema.BuildResponsePayload
                    {
                        FinishedBuilding = true
                    }
                });
            }
            return structureIdToBuildSpeed;
        }

        private void ProcessJobRequests()
        { 
            var jobRequests = commandSystem.GetRequests<StructureSchema.Structure.StartJob.ReceivedRequest>();
            for (int i = 0; i < jobRequests.Count; ++i)
            {
                ref readonly var jobRequest = ref jobRequests[i];
                workerSystem.TryGetEntity(jobRequest.EntityId, out Entity structureEntity);

                int jobId = randomNumberGenerator.Next();
                jobIdToPayload[jobId] = jobRequest.Payload.JobData;
                PostUpdateCommands.AddComponent(structureEntity, new StructureComponents.RunningJobComponent
                {
                    EstimatedJobCompletion = jobRequest.Payload.EstimatedJobCompletion,
                    JobProgress = 0,
                    JobId = jobId
                });
                commandSystem.SendResponse(new StructureSchema.Structure.StartJob.Response
                {
                    Payload = new StructureSchema.JobResponsePayload
                    {
                        JobId = jobId
                    },
                    RequestId = jobRequest.RequestId
                });
            }
        }
        #endregion
    }
}