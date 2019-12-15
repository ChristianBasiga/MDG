using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Game;
using MdgSchema.Common;
using MdgSchema.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using GameSchema = MdgSchema.Game;
using TerritorySchema = MdgSchema.Game.Territory;
namespace MDG.Game.Systems
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class GameStatusSystem : ComponentSystem
    {

        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        WorkerSystem workerSystem;
        EntityQuery gameStatusQuery;
        EntityQuery playerQuery;
        EntityQuery territoryQuery;
        EntityId gameManagerEntityId = new EntityId(-1);
        bool startedGame = false;
        bool sentStartBroadcast = false;
        GameConfig gameConfig;
        int playersJoined = 0;
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
            // Should be set by server and based on env later.
            gameConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseGameConfig") as GameConfig;
        }


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            NativeArray<SpatialEntityId> spatialEntityIds = gameStatusQuery.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
            gameManagerEntityId = spatialEntityIds[0].EntityId;
            spatialEntityIds.Dispose();
        }

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




        protected override void OnUpdate()
        {
            if (!startedGame)
            {
                var joinRequests = commandSystem.GetRequests<GameSchema.GameStatus.JoinGame.ReceivedRequest>(gameManagerEntityId);
                // Do based on role later, this is fine for now.
                playersJoined += joinRequests.Count;
                startedGame = playersJoined == gameConfig.MinimumPlayers;
                for (int i = 0; i < joinRequests.Count; ++i)
                {
                    Debug.Log("recieved join request with role " + joinRequests[i].Payload.PlayerRole);
                    commandSystem.SendResponse(new GameSchema.GameStatus.JoinGame.Response
                    {
                        RequestId = joinRequests[i].RequestId,
                        Payload = new GameSchema.PlayerJoinResponse()
                    }); 
                }
            }
            else if (!sentStartBroadcast)
            {
                componentUpdateSystem.SendEvent(new GameSchema.GameStatus.StartGame.Event(new GameSchema.StartGameEventPayload
                {
                    // Later randomly generate and components to players will have session id they belong to.
                    SessionId = 1
                }), gameManagerEntityId);
                sentStartBroadcast = true;
            }
            else
            {
                bool timedOut = false;
                int claimed = 0;
                Entities.With(territoryQuery).ForEach((ref TerritorySchema.TerritoryStatus.Component territoryStatus) =>
                {
                    if (territoryStatus.Status == TerritorySchema.TerritoryStatusTypes.Claimed)
                    {
                        claimed += 1;
                    }
                });
                if (claimed == territoryQuery.CalculateEntityCount())
                {
                    componentUpdateSystem.SendEvent(new GameSchema.GameStatus.EndGame.Event(new GameSchema.GameEndEventPayload
                    {
                        WinConditionMet = GameSchema.WinConditions.TerritoriesClaimed
                    }), gameManagerEntityId);
                }

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
            }
        }
    }
}