using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using MDG.Common.Components;
using Unity.Collections;

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
                    Debug.Log("ticking down attack cooldown");
                    c1.attackCooldown  = Mathf.Max(0, c1.attackCooldown - deltaTime);
                }
            }
        }
    }
}
