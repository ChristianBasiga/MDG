using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using MdgSchema.Common;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
namespace MDG.Game.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(GameStatusSystem))]
    // Todo: Rename to be orphan cleaner and be part of clean up system group.
    public class SystemCleanupSystem : JobComponentSystem
    {
        ComponentUpdateSystem componentUpdateSystem;
        CommandSystem commandSystem;
        EntitySystem entitySystem;
        GameStatusSystem gameStatusSystem;
        List<ComponentSystemBase> systemsToRemove;

        NativeQueue<EntityId> orphanQueue;
        JobHandle orphanCleanHandle;

        protected override void OnCreate()
        {
            base.OnCreate();
            entitySystem = World.GetExistingSystem<EntitySystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            gameStatusSystem = World.GetOrCreateSystem<GameStatusSystem>();
            orphanQueue = new NativeQueue<EntityId>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            orphanQueue.Dispose();
        }


        struct CleanUpOrphanedEntitiesJob : IJobForEach<Owner.Component, SpatialEntityId>
        {

            [ReadOnly]
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
            // Could store removed this frame to avoid iterating through entities removed again.
            List<EntityId> playersRemoved = entitySystem.GetEntitiesRemoved().Where((entityId) => !gameStatusSystem.PlayerInGame(entityId)).ToList();

            if (playersRemoved.Count == 0)
            {
                return inputDeps;
            }

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
            nativePlayersRemoved.Dispose(orphanCleanHandle);
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