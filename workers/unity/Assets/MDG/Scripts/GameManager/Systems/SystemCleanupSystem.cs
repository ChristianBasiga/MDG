using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using GameSchema = MdgSchema.Game;
using PlayerSchema = MdgSchema.Player;
using System.Linq;
using MdgSchema.Common;
using Unity.Collections;
using Unity.Jobs;
using Improbable.Gdk.Core.Commands;
// This has to be on client and server. And systems to remove is from external source depending on that upon connecting.
// So probably OnConnectionEstablished.
namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class SystemCleanupSystem : JobComponentSystem
    {
        ComponentUpdateSystem componentUpdateSystem;
        CommandSystem commandSystem;
        EntitySystem entitySystem;
        List<ComponentSystemBase> systemsToRemove;

        NativeQueue<EntityId> orphanQueue;
        JobHandle orphanCleanHandle;

        protected override void OnCreate()
        {
            base.OnCreate();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            orphanQueue = new NativeQueue<EntityId>(Allocator.Persistent);
            // Need to set all the player entity ids here.
            // have client and server references file
        }


        struct CleanUpOrphanedEntitiesJob : IJobForEach<Owner.Component, SpatialEntityId>
        {

            [DeallocateOnJobCompletion]
            public NativeHashMap<EntityId, bool> deletedOwners;

            [WriteOnly]
            public NativeQueue<EntityId>.ParallelWriter deleteQueue;

            public void Execute([ReadOnly] ref Owner.Component owner, [ReadOnly] ref SpatialEntityId spatialEntityId)
            {

                if (deletedOwners.ContainsKey(owner.OwnerId))
                {
                    deleteQueue.Enqueue(spatialEntityId.EntityId);
                }
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            List<EntityId> playersRemoved = entitySystem.GetEntitiesRemoved().Where((entityId) => 
            componentUpdateSystem.HasComponent(PlayerSchema.PlayerMetaData.ComponentId, entityId)).ToList();

            NativeHashMap<EntityId, bool> nativePlayersRemoved = new NativeHashMap<EntityId, bool>(playersRemoved.Count, Allocator.TempJob);

            for (int i = 0; i < playersRemoved.Count; ++i)
            {
                nativePlayersRemoved.TryAdd(playersRemoved[i], true);
            }
            orphanCleanHandle.Complete();
            DeleteOrphans();
            CleanUpOrphanedEntitiesJob cleanUpOrphanedEntitiesJob = new CleanUpOrphanedEntitiesJob
            {
                deletedOwners = nativePlayersRemoved,
                deleteQueue = orphanQueue.AsParallelWriter()
            };
            orphanCleanHandle = cleanUpOrphanedEntitiesJob.Schedule(this);

            return orphanCleanHandle;
        }

        private void DeleteOrphans()
        {
            while (orphanQueue.Count > 0)
            {
                EntityId entityId = orphanQueue.Dequeue();
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request
                {
                    EntityId = entityId
                });
            }
        }
    }
}