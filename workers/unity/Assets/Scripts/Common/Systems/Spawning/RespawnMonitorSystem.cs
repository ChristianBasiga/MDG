using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using SpawnSchema = MdgSchema.Common.Spawn;
using CommonSchema = MdgSchema.Common;
using Unity.Mathematics;
using Improbable;

namespace MDG.Common.Systems.Spawn
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class RespawnMonitorSystem : ComponentSystem
    {
        // This also needs event to update timer in UI before respawn happens.
        // Can't reuse delay in spawn request as also does deletion at end of ticker.
        // Can just query PendingRespawn for that actually.
        public struct RespawnPayload
        {
            public EntityId entityIdToDespawn;
            public CommonSchema.GameEntityTypes typeToSpawn;
            public int typeId;
            public Vector3f position;
        }

        //Here for now.
        readonly int maxRespawnsPerFrame = 10;
        NativeQueue<RespawnPayload> queuedRespawns;
        Dictionary<long, RespawnPayload> pendingRespawnRequests;
        EntityQuery pendingRespawnGroup;
        SpawnRequestSystem spawnRequestSystem;
        CommandSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Don't care about authority on this as noly really in client side.
            // Main reason spatialOS component is just to react to pending respawn with correct UI on each client side.
            pendingRespawnGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpawnSchema.PendingRespawn.Component>()
            );
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
                ref SpawnSchema.PendingRespawn.Component pendingRespawn, ref CommonSchema.GameMetadata.Component gameMetaData)
            {

                if (pendingRespawn.TimeTillRespawn > 0)
                {
                    pendingRespawn.TimeTillRespawn = pendingRespawn.TimeTillRespawn - deltaTime;
                }
                else
                {
                    queuedRespawns.Enqueue(new RespawnPayload
                    {
                        entityIdToDespawn = spatialEntityId.EntityId,
                        typeToSpawn = gameMetaData.Type,
                        position = pendingRespawn.PositionToRespawn,
                        typeId = gameMetaData.TypeId
                    });
                    entityCommandBuffer.RemoveComponent<SpawnSchema.PendingRespawn.Component>(index, entity);
                }
            }
        }

    

        protected override void OnUpdate()
        {
            int respawnCount = pendingRespawnGroup.CalculateEntityCount();

            if (respawnCount == 0)
            {
                return;
            }

            // Complete rest of unfinished respawns form last frame to keep things in sync.
            while (queuedRespawns.Count > 0)
            {
                SendRespawnRequest();
            }

            // Might just make persisent and allow it to finish process over athe course of few frames instead of single frame.

            TickPendingRespawnJob tickPendingRespawnJob = new TickPendingRespawnJob
            {
                queuedRespawns = queuedRespawns.AsParallelWriter(),
                entityCommandBuffer = PostUpdateCommands.ToConcurrent(),
                deltaTime = UnityEngine.Time.deltaTime
            };

            tickPendingRespawnJob.Schedule(this).Complete();
            // Send spawn requsts.
            int respawnRequestsSent = 0;
            while (queuedRespawns.Count > 0 && respawnRequestsSent < maxRespawnsPerFrame)
            {
                SendRespawnRequest();
                respawnRequestsSent += 1;
            }
        }
        private void SendRespawnRequest()
        {
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