using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using SpawnSchema = MdgSchema.Common.Spawn;
using CommonSchema = MdgSchema.Common;
using Improbable.Gdk.PlayerLifecycle;
using MdgSchema.Common;
using Unity.Collections;
using Unity.Jobs;
using System;
using MdgSchema.Units;
using EntityTemplates = MDG.Templates;
using MDG.Templates;
using MDG.DTO;

namespace MDG.Common.Systems.Spawn
{

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class SpawnRequestSystem : ComponentSystem
    {
        /* TO ADD HERE:
        - Event for showing progress on building, needed for UI to show construction of building.
        no need to show progress maybe, and can just make request, then while waiting for callback.
        pop up build in progress UI.
        - If add parameter for delay in spawning, could use that as progress
        time plus amount of frames.
        

        */
        CommandSystem commandSystem;
        // Specialized system for linking player life cycle and heart beat tracking.
        SendCreatePlayerRequestSystem sendCreatePlayerRequestSystem;
        WorkerSystem workerSystem;
        Dictionary<long, SpawnRequestHeader> requestIdToPayload;



        public class SpawnRequestPayload
        {
            public SpawnSchema.SpawnRequest payload;
            public byte[] spawnMetaData;
            public byte[] spawnData;
            public Action<EntityId> callback;
        }

        public class SpawnRequestWithDelay
        {
            public SpawnRequestPayload requestPayload;
            public float delay;
        }

        Queue<SpawnRequestPayload> spawnRequests;
        // So flow is, add to here. Create native array each frame having delays in here.
        // tick down in job, then update by index each one, checking which one to enqueue.
        List<SpawnRequestWithDelay> tickingRequests;
        public delegate void SpawnFulfilledCallback(EntityId spawned);

        public class SpawnRequestHeader
        {
            public long requestId;
            public SpawnRequestPayload requestInfo;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            tickingRequests = new List<SpawnRequestWithDelay>();
            spawnRequests = new Queue<SpawnRequestPayload>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            sendCreatePlayerRequestSystem = workerSystem.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();

            requestIdToPayload = new Dictionary<long, SpawnRequestHeader>();
        }


        public void RequestSpawn(SpawnSchema.SpawnRequest spawnRequest, Action<EntityId> spawnFulfilledCallback = null, 
            byte[] spawnMetadata = null, byte[] extraSpawnArgs = null, float delay = 0)
        {
            var payload = new SpawnRequestPayload
            {
                payload = spawnRequest,
                spawnMetaData = spawnMetadata,
                spawnData = extraSpawnArgs,
                callback = spawnFulfilledCallback,
            };
            // Don't want to enqueue this, so need to put in other list first for ticking them, then 
            // enqueue to this list afterwards. Maybe make this native queue to avoid extra iterations.
            if (delay == 0)
            {
                spawnRequests.Enqueue(payload);
            }
            else
            {
                tickingRequests.Add(new SpawnRequestWithDelay {

                    delay = delay,
                    requestPayload = payload
                });
            }
        }


        struct TickSpawnDelayJob : IJobParallelFor
        {
            public NativeArray<float> times;
            public NativeQueue<int>.ParallelWriter finishedTimes;
            public float deltaTime;
            public void Execute(int index)
            {
                times[index] -= deltaTime;
                if (times[index] <= 0)
                {
                    finishedTimes.Enqueue(index);
                }
            }
        }

        protected override void OnUpdate()
        {
            try
            {
                // Start tick job.
                NativeArray<float> timesToTick = new NativeArray<float>(tickingRequests.Count, Allocator.TempJob);

                for (int i = 0; i < tickingRequests.Count; ++i)
                {
                    timesToTick[i] = tickingRequests[i].delay;
                }

                NativeQueue<int> finishedTicks = new NativeQueue<int>(Allocator.TempJob);
                float deltaTime = UnityEngine.Time.deltaTime;
                TickSpawnDelayJob tickSpawnDelayJob = new TickSpawnDelayJob
                {
                    times = timesToTick,
                    deltaTime = deltaTime,
                    finishedTimes = finishedTicks.AsParallelWriter()
                };
                JobHandle scheduledTick = tickSpawnDelayJob.Schedule(tickingRequests.Count, 64);

                // Process requets and responses while let tick run in parralel.
                ProcessRequests();
                ProcessResponses();


                // Complete tick job if still running.
                scheduledTick.Complete();
                timesToTick.Dispose();

                // Queue for spawning the finished ticking requests
                while (finishedTicks.Count > 0)
                {
                    int finishedIndex = finishedTicks.Dequeue();
                    spawnRequests.Enqueue(tickingRequests[finishedIndex].requestPayload);
                    tickingRequests.RemoveAt(finishedIndex);
                }
                finishedTicks.Dispose();
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.Log(exception);
            }
        }



        private void ProcessRequests()
        {
            while (spawnRequests.Count > 0)
            {
                var request = spawnRequests.Dequeue();
                long requestId = -1;
                switch (request.payload.TypeToSpawn)
                {
                    case CommonSchema.GameEntityTypes.Unit:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                EntityTemplates.UnitTemplates.GetUnitEntityTemplate(workerSystem.WorkerId, 
                                (UnitTypes)request.payload.TypeId, 
                                request.payload.Position,
                                request.spawnMetaData)
                              ));
                        break;
                    case CommonSchema.GameEntityTypes.Hunted:
                    case CommonSchema.GameEntityTypes.Hunter:
                        DTO.PlayerConfig playerConfig = new DTO.PlayerConfig
                        {
                            playerType = request.payload.TypeToSpawn
                        };
                        sendCreatePlayerRequestSystem.RequestPlayerCreation(DTO.Converters.SerializeArguments(playerConfig),
                            (PlayerCreator.CreatePlayer.ReceivedResponse response) =>
                            {
                                request.callback.Invoke(response.ResponsePayload.Value.CreatedEntityId);
                            }
                            );
                        break;
                    case CommonSchema.GameEntityTypes.Resource:
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                MDG.Templates.WorldTemplates.GetResourceTemplate()
                            ));
                        break;
                    case CommonSchema.GameEntityTypes.Weapon:
                        WeaponMetadata weaponMetadata = Converters.DeserializeArguments<WeaponMetadata>(request.spawnMetaData);
                        requestId = commandSystem.SendCommand(
                            new WorldCommands.CreateEntity.Request(
                                WeaponTemplates.GetWeaponEntityTemplate(workerSystem.WorkerId, weaponMetadata.weaponType,
                                new EntityId(weaponMetadata.wielderId), request.spawnData
                                )
                            ));
                        break;
                        
                }
                if (requestId != -1)
                {
                    requestIdToPayload[requestId] = new SpawnRequestHeader
                    {
                        requestId = requestId,
                        requestInfo = request
                    };
                }
            }
        }
        private void ProcessResponses()
        {
            if (requestIdToPayload.Count == 0)
            {
                return;
            }

            // So at this point is when for sure position is set. So it is here I want to send event.
            // spawn request is client side, but collision detection is server side.
            // so i need i to be spatial event.
            var creationResponses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (int i = 0; i < creationResponses.Count; ++i)
            {
                ref readonly var response = ref creationResponses[i];
                if (requestIdToPayload.TryGetValue(response.RequestId, out SpawnRequestHeader spawnRequestHeader))
                {
                    // Callbacks are good enough to notify direct requester.
                    // but events would also be good. Hmm I mean if Invader side is one making the request
                    // can update unit count accordingly. Former more general and expansive
                    // but latter is enough.
                    switch (response.StatusCode)
                    {
                        // Remove from request mappings and send response back.
                        case StatusCode.Success:
                            // This callback. Instead of sending  event further.
                            // Could attach in callback the call to update hud would be cleanest.
                            spawnRequestHeader.requestInfo.callback?.Invoke(response.EntityId.Value);
                            requestIdToPayload.Remove(response.RequestId);
                            break;
                        case StatusCode.Timeout:
                            // If time out try again on this side, again need to set up generic way of doing these retries.
                            break;
                        default:
                            commandSystem.SendResponse(new SpawnSchema.SpawnManager.SpawnGameEntity.Response
                            {
                                RequestId = spawnRequestHeader.requestId,
                                FailureMessage = $"{response.StatusCode.ToString()}: {response.Message}"
                            });
                            break;
                    }
                }
            }
        }
    }
}