using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;
using CollisionSchema = MdgSchema.Common.Collision;

namespace MDG.Defender.Systems
{
    [DisableAutoCreation]
    public class TrapMonitorSystem : ComponentSystem
    {

        ComponentUpdateSystem componentUpdateSystem;
        EntityQuery trapQuery;

        List<EntityId> trapIds;

        // While fast query, getting all traps is kinda of eh.
        struct GetTrapsJob : IJobForEachWithEntity<SpatialEntityId, StructureSchema.Trap.Component>
        {
            public NativeArray<EntityId> trapIds;
            public void Execute(Entity entity, int index, [ReadOnly] ref SpatialEntityId c0, [ReadOnly] ref StructureSchema.Trap.Component c1)
            {
                trapIds[index] = c0.EntityId;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            trapIds = new List<EntityId>();
            trapQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<StructureSchema.Trap.Component>());
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        }

        protected override void OnUpdate()
        {
            int amountOfTraps = trapQuery.CalculateEntityCount();
            if (amountOfTraps == 0)
            {
                return;
            }
            NativeArray<EntityId> trapIds = new NativeArray<EntityId>(amountOfTraps, Allocator.TempJob);
            GetTrapsJob getTrapsJob = new GetTrapsJob
            {
                trapIds = trapIds
            };
            getTrapsJob.Schedule(trapQuery).Complete();

        }
    }
}