using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using StatSchema = MdgSchema.Common.Stats;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using Improbable.Gdk.Core.Commands;

namespace MDG.Common.Systems.Stat
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class StatMonitorSystem : JobComponentSystem
    {
        EntityQuery healthMonitorQuery;
        EntityQuery applyDamageQuery;
        CommandSystem commandSystem;
        WorkerSystem workerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            healthMonitorQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<StatSchema.Stats.Component>(),
                ComponentType.Exclude<JustDied>()
                );

            applyDamageQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<StatSchema.Stats.Component>(),
                ComponentType.ReadOnly<StatSchema.Stats.ComponentAuthority>()
                );

            applyDamageQuery.SetFilter(StatSchema.Stats.ComponentAuthority.Authoritative);

            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        struct MonitorHealthJob : IJobForEach<SpatialEntityId, StatSchema.Stats.Component>
        {
            public NativeQueue<EntityId>.ParallelWriter dead;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref StatSchema.Stats.Component stats)
            {
                if (stats.Health == 0)
                {
                    dead.Enqueue(spatialEntityId.EntityId);
                }
            }
        }

        struct ApplyDamageJob : IJobForEach<SpatialEntityId, StatSchema.Stats.Component>
        {
            [ReadOnly]
            public NativeHashMap<EntityId, int> idToDamage;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, ref StatSchema.Stats.Component statComponent)
            {
                if (idToDamage.TryGetValue(spatialEntityId.EntityId, out int damage))
                {
                    statComponent.Health = Mathf.Max(0, statComponent.Health - damage);
                }
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            NativeQueue<EntityId> dead = new NativeQueue<EntityId>(Allocator.TempJob);
            MonitorHealthJob monitorHealthJob = new MonitorHealthJob
            {
                dead = dead.AsParallelWriter()
            };
            JobHandle monitorHealthJobHandle = monitorHealthJob.Schedule(this);

            // Looping through ids might be slower depending on how espensive getrequests is.
            #region Process Damage Requests
            var damageRequests = commandSystem.GetRequests<StatSchema.Stats.DamageEntity.ReceivedRequest>();
            NativeHashMap<EntityId, int> entityIdToDamage = new NativeHashMap<EntityId, int>(damageRequests.Count, Allocator.TempJob);

            for (int i = 0; i < damageRequests.Count; ++i)
            {
                Debug.Log("recieved damage request");
                ref readonly var damageRequest = ref damageRequests[i];
                workerSystem.TryGetEntity(damageRequest.EntityId, out Entity entity);
                StatSchema.Stats.Component statComponent = EntityManager.GetComponentData<StatSchema.Stats.Component>(entity);

                // If already dead, send response now that dead
                if (statComponent.Health <= 0)
                {
                    commandSystem.SendResponse(new StatSchema.Stats.DamageEntity.Response
                    {
                        RequestId = damageRequest.RequestId,
                        Payload = new StatSchema.DamageResponse
                        {
                            AlreadyDead = true,
                            Killed = false
                        }
                    });
                }
                else
                {
                    // Down line, mutate this as needed by shoving it down a chain of command.
                    // as this damage is base with nothing else considered.
                    int damageToApply = damageRequest.Payload.Damage;

                    if (entityIdToDamage.TryGetValue(damageRequest.EntityId, out int damageQueued))
                    {
                        entityIdToDamage[damageRequest.EntityId] = damageQueued + damageToApply;
                    }
                    else
                    {
                        entityIdToDamage.TryAdd(damageRequest.EntityId, damageToApply);
                    }

                    if (statComponent.Health - entityIdToDamage[damageRequest.EntityId] <= 0)
                    {
                        UnityEngine.Debug.Log("Sending killed response");
                        commandSystem.SendResponse(new StatSchema.Stats.DamageEntity.Response
                        {
                            RequestId = damageRequest.RequestId,
                            Payload = new StatSchema.DamageResponse
                            {
                                AlreadyDead = false,
                                Killed = true
                            }
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Sending still alive response");
                        commandSystem.SendResponse(new StatSchema.Stats.DamageEntity.Response
                        {
                            RequestId = damageRequest.RequestId,
                            Payload = new StatSchema.DamageResponse
                            {
                                AlreadyDead = false,
                                Killed = false
                                
                            }
                        });
                    }
                }
            }
            #endregion
            monitorHealthJobHandle.Complete();

            // Start thread for apply damage job while we delete killed last frame.
            ApplyDamageJob applyDamageJob = new ApplyDamageJob
            {
                idToDamage = entityIdToDamage
            };

            JobHandle applyDamageJobHandle = applyDamageJob.Schedule(applyDamageQuery);

            #region Dequeue killed entities last frame and send delete requests to remove from all clients.
            while (dead.Count > 0)
            {
                EntityId killed_id = dead.Dequeue();
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
                {
                    EntityId = killed_id
                });
            }
            dead.Dispose();
            #endregion
            applyDamageJobHandle.Complete();
            entityIdToDamage.Dispose();
            return inputDeps;
        }
    }
}