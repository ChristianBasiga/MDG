using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common.Components;
using ResourceSchema = MdgSchema.Game.Resource;
using System;
using MdgSchema.Common.Spawn;
using Unity.Jobs;
using MdgSchema.Common;
using Unity.Collections;

namespace MDG.Invader.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ResourceMonitorSystem : ComponentSystem
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        JobHandle respawnJobHandle;
        EntityQuery respawnResourceGroup;

        struct RespawnResourceJob : IJobForEachWithEntity<EntityTransform.Component, ResourceSchema.Resource.Component, ResourceSchema.ResourceMetadata.Component>
        {
            public EntityCommandBuffer.Concurrent commandBuffer;
            public void Execute(Entity entity, int index, [ReadOnly] ref EntityTransform.Component entityTransform, [ReadOnly] ref ResourceSchema.Resource.Component resourceComponent,
                [ReadOnly] ref ResourceSchema.ResourceMetadata.Component resourceMetadata)
            {
                if (resourceComponent.Health == 0)
                {
                    if (resourceMetadata.WillRespawn)
                    {
                        commandBuffer.AddComponent(index, entity, new PendingRespawn.Component
                        {
                            PositionToRespawn = entityTransform.Position,
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
                ComponentType.ReadOnly<EntityTransform.Component>(),
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