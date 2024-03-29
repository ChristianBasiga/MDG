﻿using Improbable.Gdk.Core;
using MDG.Common.Components;
using MdgSchema.Common.Util;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using CollisionSchema = MdgSchema.Common.Collision;
using CommonSchema = MdgSchema.Common;

namespace MDG.Common.Jobs
{
    public class ClientJobs
    {
        // Multiple diff systems may run this. So having specifcially just jobs namespace will be better
        // reuse.
        public struct TickAttackCooldownJob : IJobForEach<CombatMetadata, CombatStats>
        {
            public float deltaTime;
            public void Execute([ReadOnly] ref CombatMetadata c0, ref CombatStats c1)
            {
                if (c1.attackCooldown > 0)
                {
                    c1.attackCooldown  = Mathf.Max(0, c1.attackCooldown - deltaTime);
                    // We ONly tick attack cooldown, do nothing to range so it should remain the same, why would it ever reach 0.
                }
            }
        }


        public struct RaycastHit
        {
            public EntityId entityId;
            public Vector3f position;
        }

        public struct Raycast : IJobForEach<SpatialEntityId, CollisionSchema.BoxCollider.Component, CommonSchema.EntityPosition.Component>
        {
            // REplace this later with hit struct containing both id and position.
            public NativeQueue<RaycastHit>.ParallelWriter hits;
            public EntityId checking;
            public Vector3f startPoint;
            public Vector3f endPoint;

            // Welp time to debug this...
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref CollisionSchema.BoxCollider.Component boxCollider, [ReadOnly] ref CommonSchema.EntityPosition.Component EntityPosition)
            {

                if (spatialEntityId.EntityId.Equals(checking) || boxCollider.IsTrigger)
                {
                    return;
                }
                float slope = HelperFunctions.Slope(startPoint, endPoint);
                // Tolerance of seeing on same line will be the collider for ray cast.
                Debug.Log("checking if ray cast hit id " + spatialEntityId.EntityId);
                if (Mathf.Abs(slope * (EntityPosition.Position.X - startPoint.X) - EntityPosition.Position.Z) <= HelperFunctions.Magnitude(boxCollider.Dimensions))
                {
                    Debug.Log($"Within line of sight is entity with id {spatialEntityId.EntityId}");
                    hits.Enqueue(new RaycastHit
                    {
                        entityId = spatialEntityId.EntityId,
                        position = EntityPosition.Position
                    });
                }
            }
        }
    }
}
