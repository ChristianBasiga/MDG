using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using MDG.Common.Components;
using CommonSchema = MdgSchema.Common;
using StatSchema = MdgSchema.Common.Stats;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;

namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    //[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class TimeManagementSystem : JobComponentSystem
    {
        CommandSystem commandSystem;
        EntityQuery timeLimitedAuth;
        EntityQuery combatStatsQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            timeLimitedAuth = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CommonSchema.TimeLimitation.ComponentAuthority>(),
                ComponentType.ReadWrite<CommonSchema.TimeLimitation.Component>()
                );
            timeLimitedAuth.SetFilter(CommonSchema.TimeLimitation.ComponentAuthority.Authoritative);
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }
        struct TickTimeLimitedComponentsJob : IJobForEachWithEntity<SpatialEntityId, CommonSchema.TimeLimitation.Component>
        {
            public float deltaTime;
            public NativeArray<EntityId> toRemove;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId spatialEntityId, ref CommonSchema.TimeLimitation.Component c0)
            {
                c0.TimeLeft -= deltaTime;
                if (c0.TimeLeft <= 0)
                {
                    toRemove[index] = spatialEntityId.EntityId;
                }
            }
        }




        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float deltaTime = UnityEngine.Time.deltaTime;
            NativeArray<EntityId> toRemove = new NativeArray<EntityId>(timeLimitedAuth.CalculateEntityCount(), Allocator.TempJob);
            TickTimeLimitedComponentsJob tickTimeLimitedComponentsJob = new TickTimeLimitedComponentsJob
            {
                deltaTime = deltaTime,
                toRemove = toRemove
            };
            JobHandle tickTimeLimitedHandle = tickTimeLimitedComponentsJob.Schedule(timeLimitedAuth);




            // Since queues stuff in buffer must complete this frame so in sync.
            tickTimeLimitedHandle.Complete();
            for (int i = 0; i < toRemove.Length; ++i)
            {
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
                {
                    EntityId = toRemove[i]
                });
            }
            toRemove.Dispose();
            return inputDeps;
        }
    }
}