using Improbable.Gdk.Core;
using MdgSchema.Common;
using MdgSchema.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using GameSchema = MdgSchema.Game;
using TerritorySchema = MdgSchema.Game.Territory;
namespace MDG.Game
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class GameStatusSystem : ComponentSystem
    {

        readonly int minPlayers = 1;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        WorkerSystem workerSystem;
        EntityQuery gameStatusQuery;
        EntityQuery playerQuery;
        EntityQuery territoryQuery;
        readonly int minPlayersToStart;
        readonly int territoriesCount;
        bool startedGame = false;


        struct CheckEnoughPlayersJob : IJobForEach<PlayerMetaData.Component, GameMetadata.Component>
        {
            public NativeArray<bool> invaderSpawned;
            public NativeArray<int> defendersSpawnedCount;
            public void Execute([ReadOnly] ref PlayerMetaData.Component c0, [ReadOnly] ref GameMetadata.Component c1)
            {
                switch (c1.Type)
                {
                    case GameEntityTypes.Hunted:
                        defendersSpawnedCount[0] += 1;
                        break;
                    case GameEntityTypes.Hunter:
                        invaderSpawned[0] = true;
                        break;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            territoryQuery = GetEntityQuery(
                ComponentType.ReadOnly<TerritorySchema.Territory.Component>(),
                ComponentType.ReadOnly<TerritorySchema.TerritoryStatus.Component>()
                );
            playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerMetaData.Component>());
            gameStatusQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<GameSchema.GameStatus.Component>(),
                ComponentType.ReadOnly<GameSchema.GameStatus.ComponentAuthority>()
                );
            gameStatusQuery.SetFilter(GameSchema.GameStatus.ComponentAuthority.Authoritative);
            commandSystem = World.GetExistingSystem<CommandSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override void OnUpdate()
        {
            // Will validate and limit player choice prior, so simply getting the count is enough.
            // Four clients for 3 defenders and 1 invader.
            bool startGame = playerQuery.CalculateEntityCount() == minPlayers;
           // startedGame = playersSpawned == minPlayersToStart;
           // Store entityId of core entities.
            // Should only be started once both invader and defender are in the world.
            if (!startedGame && startGame)
            {
                startedGame = true;     

                // replace this literal.
                workerSystem.TryGetEntity(new EntityId(2), out Unity.Entities.Entity gameManagerEntity);
                // Strangely enough, snapshot setting this alo to 900 not working. Hmm

                // Short to test this.
                EntityManager.SetComponentData(gameManagerEntity, new GameSchema.GameStatus.Component
                {
                    TimeLeft = 100.0f
                });
                return;
            }


            if (startedGame)
            {
                bool timedOut = false;
                Entities.With(gameStatusQuery).ForEach((ref SpatialEntityId spatialEntityId, ref GameSchema.GameStatus.Component gameStatus) =>
                {
                    if (gameStatus.TimeLeft > 0)
                    {
                        gameStatus.TimeLeft = max(gameStatus.TimeLeft - UnityEngine.Time.deltaTime, 0);
                    }
                    else if (!timedOut)
                    {
                        timedOut = true;
                        // Gotta rise game over event.
                        componentUpdateSystem.SendEvent(new GameSchema.GameStatus.EndGame.Event(new GameSchema.GameEndEventPayload
                        {
                            WinConditionMet = GameSchema.WinConditions.TimedOut
                        }), spatialEntityId.EntityId);
                    }
                });

                int claimed = 0;
                Entities.With(territoryQuery).ForEach((ref TerritorySchema.TerritoryStatus.Component territoryStatus) =>
                {
                    if (territoryStatus.Status == TerritorySchema.TerritoryStatusTypes.Claimed)
                    {
                        claimed += 1;
                    }
                });
                // If all territories are claimed.
                if (claimed == territoryQuery.CalculateEntityCount())
                {
                    componentUpdateSystem.SendEvent(new GameSchema.GameStatus.EndGame.Event(new GameSchema.GameEndEventPayload
                    {
                        WinConditionMet = GameSchema.WinConditions.TerritoriesClaimed
                    }), new EntityId(3));
                }

                // Then also need to check invader win condition. All three territories are claimed.
            }
        }
    }
}