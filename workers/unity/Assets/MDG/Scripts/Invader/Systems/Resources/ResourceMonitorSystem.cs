using Improbable.Gdk.Core;
using MdgSchema.Common;
using MdgSchema.Common.Spawn;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using ResourceSchema = MdgSchema.Game.Resource;

namespace MDG.Invader.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ResourceMonitorSystem : ComponentSystem
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        JobHandle respawnJobHandle;
        EntityQuery respawnResourceGroup;

        struct RespawnResourceJob : IJobForEachWithEntity<EntityPosition.Component, ResourceSchema.Resource.Component, ResourceSchema.ResourceMetadata.Component>
        {
            public EntityCommandBuffer.Concurrent commandBuffer;
            public void Execute(Entity entity, int index, [ReadOnly] ref EntityPosition.Component EntityPosition, [ReadOnly] ref ResourceSchema.Resource.Component resourceComponent,
                [ReadOnly] ref ResourceSchema.ResourceMetadata.Component resourceMetadata)
            {
                if (resourceComponent.Health == 0)
                {
                    if (resourceMetadata.WillRespawn)
                    {
                        commandBuffer.AddComponent(index, entity, new PendingRespawn.Component
                        {
                            PositionToRespawn = EntityPosition.Position,
                            TimeTillRespawn = resourceMetadata.RespawnTime
                        });
                    }
                    else
                    {
                        commandBuffer.DestroyEntity(index, entity);
                    }
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            respawnResourceGroup = GetEntityQuery(
                ComponentType.Exclude<PendingRespawn.Component>(),
                ComponentType.ReadOnly<EntityPosition.Component>(),
                ComponentType.ReadOnly<ResourceSchema.Resource.Component>(),
                ComponentType.ReadOnly<ResourceSchema.ResourceMetadata.Component>());
          //  entityCommandBufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            RespawnResourceJob respawnResourceJob = new RespawnResourceJob
            {
                commandBuffer = PostUpdateCommands.ToConcurrent()
            };
            respawnJobHandle = respawnResourceJob.Schedule(respawnResourceGroup);
            respawnJobHandle.Complete();
        }
    }
}