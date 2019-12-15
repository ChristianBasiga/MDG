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
using MdgSchema.Common.Stats;
using MDG.Common.Systems.Position;
using MdgSchema.Common.Util;

namespace MDG.Common.Systems.Collision {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(PositionSystem))]
    [AlwaysUpdateSystem]
    public class CollisionHandlerSystem : JobComponentSystem
    {
        EntityQuery authoritativeVelocityGroup;
        EntityQuery authPosGroup;
        EntityQuery collisionsGroup;
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
             ComponentType.ReadWrite<EntityPosition.Component>(),
             ComponentType.ReadOnly<EntityPosition.ComponentAuthority>(),
             ComponentType.ReadOnly<MovementSpeed.Component>()
             );
            authPosGroup.SetFilter(EntityPosition.ComponentAuthority.Authoritative);

            collisionsGroup = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CollisionSchema.Collision.Component>(),
                ComponentType.ReadOnly<CollisionSchema.BoxCollider.Component>(),
                ComponentType.ReadOnly<PositionSchema.LinearVelocity.Component>());

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }


        // Unjobifying this for interest of cross referencing
        struct GetCollisionsJob : IJobForEachWithEntity<SpatialEntityId, CollisionSchema.Collision.Component, CollisionSchema.BoxCollider.Component, PositionSchema.LinearVelocity.Component>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, bool>.ParallelWriter toReverse;

            public void Execute( Entity entity, int jobIndex, [ReadOnly] ref SpatialEntityId c0, [ReadOnly] ref CollisionSchema.Collision.Component c1, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider,
                [ReadOnly] ref PositionSchema.LinearVelocity.Component linearVelocityComponent)
            {
                Vector3 linearVelocity = HelperFunctions.Vector3fToVector3(linearVelocityComponent.Velocity).normalized;

                if (!boxCollider.IsTrigger && c1.CollisionCount > 0 && !HelperFunctions.Equals(linearVelocityComponent.Velocity, new Vector3f(0,0,0)))
                {
                    bool add = false;
                    foreach (var key in c1.Collisions.Keys)
                    {
                        if (c1.Collisions[key].IsTrigger)
                        {
                            continue;
                        }
                        // Check if velocity is tending same direction as distance to collision.
                        // Replace all normalized with unitisdes
                        // If it it's not, then don't undo the position update. Otherwise if does tend in same direction, then will contine collision.
                        Vector3 distanceVector = HelperFunctions.Vector3fToVector3(c1.Collisions[key].Distance).normalized;
                        float dotProduct = Vector3.Dot(linearVelocity, distanceVector);
                        if (dotProduct > 0.7f)
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

        struct UndoPositionChangeJob : IJobForEach<SpatialEntityId, PositionSchema.LinearVelocity.Component, EntityPosition.Component,
            MovementSpeed.Component>
        {
            public float deltaTime;
            [ReadOnly]
            public NativeHashMap<EntityId, bool> toReverse;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref PositionSchema.LinearVelocity.Component velocityComponent,
                ref EntityPosition.Component EntityPositionComponent, [ReadOnly] ref MovementSpeed.Component movementSpeed)
            {
                if (toReverse.TryGetValue(spatialEntityId.EntityId, out bool _))
                {
                    EntityPositionComponent.Position =  HelperFunctions.Subtract(EntityPositionComponent.Position, 
                        HelperFunctions.Scale(HelperFunctions.Normalize(velocityComponent.Velocity), deltaTime * movementSpeed.LinearSpeed));
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float deltaTime = Time.deltaTime;
            toReverse = new NativeHashMap<EntityId, bool>(authPosGroup.CalculateEntityCount(), Allocator.TempJob);

            // Ohhh even handler is bad, cause need to cross reference the to reverse
            GetCollisionsJob getCollisionsJob = new GetCollisionsJob
            {
                toReverse = toReverse.AsParallelWriter()
            };

            JobHandle jobHandle = getCollisionsJob.Schedule(collisionsGroup);

            UndoPositionChangeJob undoPositionChangeJob = new UndoPositionChangeJob
            {
                deltaTime = deltaTime,
                toReverse = toReverse
            };

            undoPositionJobHandle = undoPositionChangeJob.Schedule(authPosGroup, jobHandle);
            undoPositionJobHandle.Complete();
            toReverse.Dispose();

            return inputDeps;
        }
    }
}