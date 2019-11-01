using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable.Gdk.Core;
using CollisionSchema = MdgSchema.Common.Collision;
using PositionSchema = MdgSchema.Common.Position;
using Improbable;
using MdgSchema.Common;

namespace MDG.Common.Systems.Collision {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(CollisionDetectionSystem))]
    [AlwaysUpdateSystem]
    public class CollisionHandlerSystem : JobComponentSystem
    {
        EntityQuery authoritativeVelocityGroup;
        EntityQuery authPosGroup;
        NativeHashMap<EntityId, bool> toReverse;
        JobHandle undoPositionJobHandle;
        ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            authPosGroup = GetEntityQuery(
             ComponentType.ReadOnly<SpatialEntityId>(),
             ComponentType.ReadOnly<PositionSchema.LinearVelocity.Component>(),
             ComponentType.ReadOnly<PositionSchema.AngularVelocity.Component>(),
             ComponentType.ReadWrite<EntityTransform.Component>(),
             ComponentType.ReadOnly<EntityTransform.ComponentAuthority>()
             );
            authPosGroup.SetFilter(EntityTransform.ComponentAuthority.Authoritative);
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        struct GetCollisionsJob : IJobForEach<SpatialEntityId, CollisionSchema.Collision.Component, CollisionSchema.BoxCollider.Component, PositionSchema.LinearVelocity.Component>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, bool>.ParallelWriter toReverse;
            public void Execute([ReadOnly] ref SpatialEntityId c0, [ReadOnly] ref CollisionSchema.Collision.Component c1, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                [ReadOnly] ref PositionSchema.LinearVelocity.Component linearVelocity)
            {
                if (!boxCollider.IsTrigger && c1.Collisions.Count > 0)
                {
                    bool add = false;
                    foreach (var key in c1.Collisions.Keys)
                    {
                        // Check if velocity is tending same direction as distance to collision.
                        // Replace all normalized with unitisdes
                        // If it it's not, then don't undo the position update. Otherwise if does tend in same direction, then will contine collision.
                        float dotProduct = Vector3.Dot(linearVelocity.Velocity.ToUnityVector().normalized, c1.Collisions[key].Distance.ToUnityVector().normalized);
                        if (dotProduct > 0)
                        {
                            add = true;
                            break;
                        }
                    }
                    if (add)
                    {
                        toReverse.TryAdd(c0.EntityId, true);
                    }
                }
            }
        }

        struct UndoPositionChangeJob : IJobForEach<SpatialEntityId, PositionSchema.LinearVelocity.Component, EntityTransform.Component>
        {
            public float deltaTime;
            [ReadOnly]
            public NativeHashMap<EntityId, bool> toReverse;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref PositionSchema.LinearVelocity.Component velocityComponent,
                ref EntityTransform.Component entityTransformComponent)
            {
                if (toReverse.TryGetValue(spatialEntityId.EntityId, out bool _))
                {
                    entityTransformComponent.Position -= (velocityComponent.Velocity * deltaTime);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float deltaTime = Time.deltaTime;
            toReverse = new NativeHashMap<EntityId, bool>(authPosGroup.CalculateEntityCount(), Allocator.TempJob);
            GetCollisionsJob getCollisionsJob = new GetCollisionsJob
            {
                toReverse = toReverse.AsParallelWriter()
            };

            inputDeps = getCollisionsJob.Schedule(this, inputDeps);

            UndoPositionChangeJob undoPositionChangeJob = new UndoPositionChangeJob
            {
                deltaTime = deltaTime,
                toReverse = toReverse
            };
            inputDeps.Complete();
            undoPositionJobHandle = undoPositionChangeJob.Schedule(authPosGroup, inputDeps);
            undoPositionJobHandle.Complete();
            toReverse.Dispose();
            return undoPositionJobHandle;
        }
    }
}