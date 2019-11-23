using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using SpawnSchema = MdgSchema.Common.Spawn;
using PositionSchema = MdgSchema.Common.Position;
using StatSchema = MdgSchema.Common.Stats;
using CommonSchema = MdgSchema.Common;
using Unity.Mathematics;
using Improbable;

namespace MDG.Common.Systems.Spawn
{
    // Shold make this sit in server instead.
    // With that said, maybe not actually delete / create. Just request position change. Then mono
    // can simply make it inactive for the moment by checking for pending respawn.

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(Stat.StatMonitorSystem))]
    public class RespawnMonitorSystem : ComponentSystem
    {
        public struct RespawnPayload
        {
            public EntityId entityIdToDespawn;
            public CommonSchema.GameEntityTypes typeToSpawn;
            public int typeId;
            public Vector3f position;
        }

        //Here for now.
        NativeQueue<RespawnPayload> queuedRespawns;
        Dictionary<long, RespawnPayload> pendingRespawnRequests;
        EntityQuery pendingRespawnGroup;
        SpawnRequestSystem spawnRequestSystem;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        WorkerSystem workerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Don't care about authority on this as noly really in client side.
            // Main reason spatialOS component is just to react to pending respawn with correct UI on each client side.
            pendingRespawnGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<SpawnSchema.PendingRespawn.Component>(),
                ComponentType.ReadOnly<SpawnSchema.PendingRespawn.ComponentAuthority>(),
                ComponentType.ReadOnly<CommonSchema.GameMetadata.Component>()
            );
            pendingRespawnGroup.SetFilter(SpawnSchema.PendingRespawn.ComponentAuthority.Authoritative);
            pendingRespawnGroup.SetFilterChanged(ComponentType.ReadWrite<SpawnSchema.PendingRespawn.Component>());

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();


            commandSystem = World.GetExistingSystem<CommandSystem>();
            spawnRequestSystem = World.GetExistingSystem<SpawnRequestSystem>();
            pendingRespawnRequests = new Dictionary<long, RespawnPayload>();
            queuedRespawns = new NativeQueue<RespawnPayload>(Allocator.Persistent);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            queuedRespawns.Dispose();
        }

        public struct TickPendingRespawnJob : IJobForEachWithEntity<SpatialEntityId, SpawnSchema.PendingRespawn.Component, CommonSchema.GameMetadata.Component>
        {
            public NativeQueue<RespawnPayload>.ParallelWriter queuedRespawns;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            public float deltaTime;
            public void Execute(Unity.Entities.Entity entity, int index, [ReadOnly] ref SpatialEntityId spatialEntityId, 
                ref SpawnSchema.PendingRespawn.Component pendingRespawn, [ReadOnly] ref CommonSchema.GameMetadata.Component gameMetaData)
            {
                if (!pendingRespawn.RespawnActive) return;


                if (pendingRespawn.TimeTillRespawn > 0)
                {
                    UnityEngine.Debug.Log($"Ticking respawn {pendingRespawn.TimeTillRespawn}");
                    pendingRespawn.TimeTillRespawn = pendingRespawn.TimeTillRespawn - deltaTime;
                }
                else
                {

                    UnityEngine.Debug.Log("Triggering respawn");

                    // Triggers respawn.
                    pendingRespawn.RespawnActive = false;

                    queuedRespawns.Enqueue(new RespawnPayload
                    {
                        entityIdToDespawn = spatialEntityId.EntityId,
                        typeToSpawn = gameMetaData.Type,
                        position = pendingRespawn.PositionToRespawn,
                        typeId = gameMetaData.TypeId
                    });
                }
            }
        }

    

        protected override void OnUpdate()
        {

            TickPendingRespawnJob tickPendingRespawnJob = new TickPendingRespawnJob
            {
                queuedRespawns = queuedRespawns.AsParallelWriter(),
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                deltaTime = UnityEngine.Time.deltaTime
            };

            tickPendingRespawnJob.Schedule(pendingRespawnGroup).Complete();

            while (queuedRespawns.Count > 0)
            {
                RespawnPayload respawnPayload = queuedRespawns.Dequeue();
                componentUpdateSystem.SendUpdate(new CommonSchema.EntityTransform.Update
                {
                    Position = respawnPayload.position
                }, respawnPayload.entityIdToDespawn);

                workerSystem.TryGetEntity(respawnPayload.entityIdToDespawn, out Unity.Entities.Entity respawnedEntity);

                // I should abuse queries more. Not fuly utilizing spatialOS tools.
                StatSchema.StatsMetadata.Component statsMetadata = EntityManager.GetComponentData<StatSchema.StatsMetadata.Component>(respawnedEntity);

                componentUpdateSystem.SendUpdate(new StatSchema.Stats.Update
                {
                    Health = statsMetadata.Health
                }, respawnPayload.entityIdToDespawn);
            }


            /*
            // Send spawn requsts.
            int respawnRequestsSent = 0;
            while (queuedRespawns.Count > 0 && respawnRequestsSent < maxRespawnsPerFrame)
            {
                SendRespawnRequest();
                respawnRequestsSent += 1;
            }*/
        }
        // Deprecated... Ahh refactor
        private void SendRespawnRequest()
        {
            // This needs to change. Not deleting, simply set respawn active to false.
            if (queuedRespawns.TryDequeue(out RespawnPayload payload))
            {
                var deleteEntityRequest = new WorldCommands.DeleteEntity.Request(payload.entityIdToDespawn);
                commandSystem.SendCommand(deleteEntityRequest);
                spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
                {
                    TypeToSpawn = payload.typeToSpawn,
                    Position = payload.position
                });
            }
        }

        private void ProcessRespawnResponses()
        {
            var responses = commandSystem.GetResponses<SpawnSchema.SpawnManager.SpawnGameEntity.ReceivedResponse>();
            for (int i = 0; i < responses.Count; ++i)
            {
                ref readonly var response = ref responses[i];
                switch (response.StatusCode)
                {
                    case StatusCode.Success:
                        // This is why, removing from pending keys. I mean double my responses versus
                        // once all responses, what is more.
                        pendingRespawnRequests.Remove(response.RequestId);
                        break;

                    case StatusCode.Timeout:

                        // Actually for dealing with timeouts, it really does need to be persisent.
                        queuedRespawns.Enqueue(new RespawnPayload
                        {
                            typeToSpawn = response.RequestPayload.TypeToSpawn,
                            position = response.RequestPayload.Position
                        });
                        break;
                    default:

                        break;
                }
            }
        }
    }
}