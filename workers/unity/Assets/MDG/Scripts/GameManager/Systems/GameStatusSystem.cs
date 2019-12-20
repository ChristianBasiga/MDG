using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Game;
using MdgSchema.Common;
using MdgSchema.Player;
using System.Collections.Generic;
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
    [UpdateBefore(typeof(SystemCleanupSystem))]
    public class GameStatusSystem : ComponentSystem
    {
        private ILogDispatcher logger;


        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;
        WorkerSystem workerSystem;
        EntitySystem entitySystem;

        EntityQuery gameStatusQuery;
        EntityQuery playerQuery;
        EntityQuery territoryQuery;
        EntityId gameManagerEntityId = new EntityId(-1);
        HashSet<EntityId> playerIds;


        // These flags are not scalable for multiple sessions at once, replace later.
        bool startedGame = false;
        bool sentStartBroadcast = false;
        bool endedGame = false;
        GameConfig gameConfig;


        #region public methods

        public bool PlayerInGame(EntityId entityId)
        {
            return playerIds.Contains(entityId);
        }
        #endregion

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
            entitySystem = World.GetExistingSystem<EntitySystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            // Should be set by server and based on env later.
            gameConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseGameConfig") as GameConfig;
            logger = workerSystem.LogDispatcher;
            playerIds = new HashSet<EntityId>();
        }


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            NativeArray<SpatialEntityId> spatialEntityIds = gameStatusQuery.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
            gameManagerEntityId = spatialEntityIds[0].EntityId;
            spatialEntityIds.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!startedGame)
            {
                var joinRequests = commandSystem.GetRequests<GameSchema.GameStatus.JoinGame.ReceivedRequest>(gameManagerEntityId);
                // Do based on role later, this is fine for now.
                for (int i = 0; i < joinRequests.Count; ++i)
                {
                    ref readonly var joinRequest = ref joinRequests[i];
                    Debug.Log($"recieved join request with role {joinRequest.Payload.PlayerRole} and id {joinRequest.Payload.EntityId}");
                    if (playerIds.Contains(joinRequest.Payload.EntityId))
                    {
                        /* Make own log dispatcher extending SpatialOS logger later.
                        logger.HandleLog(LogType.Log, new LogEvent
                        {
                            f
                        })*/
                        string message = $"{joinRequest.Payload.EntityId} has already joined the game";
                        Debug.Log(message);
                        commandSystem.SendResponse(new GameSchema.GameStatus.JoinGame.Response
                        {
                            FailureMessage = message,
                            RequestId = joinRequest.RequestId
                        });
                    }
                    else
                    {
                        commandSystem.SendResponse(new GameSchema.GameStatus.JoinGame.Response
                        {
                            RequestId = joinRequest.RequestId,
                            Payload = new GameSchema.PlayerJoinResponse
                            {
                                EntityId = joinRequest.Payload.EntityId,
                                PlayerRole = joinRequest.Payload.PlayerRole
                            }
                        });
                        playerIds.Add(joinRequest.Payload.EntityId);
                    }
                }
                startedGame = playerIds.Count.Equals(gameConfig.MinimumPlayers);
            }
            else if (!sentStartBroadcast)
            {
                componentUpdateSystem.SendEvent(new GameSchema.GameStatus.StartGame.Event(new GameSchema.StartGameEventPayload
                {
                    // Later randomly generate and components to players will have session id they belong to.
                    SessionId = 1
                }), gameManagerEntityId);
                sentStartBroadcast = true;

                componentUpdateSystem.SendUpdate(new GameSchema.GameStatus.Update
                {
                    GameState = GameSchema.GameStates.Playing
                }, gameManagerEntityId);

                // Right now it only sends events not actually maintain state initself.
            }
            else if (!endedGame)
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
                    OnEndGame();
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
                            WinConditionMet = GameSchema.WinConditions.TimedOut,
                            GameState = GameSchema.GameStates.Over
                        }), spatialEntityId.EntityId);
                        OnEndGame();
                    }
                });
            }
        }

        private void CheckDeletedPlayers()
        {
            List<EntityId> deletedPlayers = entitySystem.GetEntitiesRemoved().FindAll(id => playerIds.Contains(id));
            for (int i = 0; i < deletedPlayers.Count; ++i)
            {
                playerIds.Remove(deletedPlayers[i]);
            }
        }

        private void OnEndGame()
        {
            endedGame = true;
            componentUpdateSystem.SendUpdate(new GameSchema.GameStatus.Update
            {
                GameState = GameSchema.GameStates.Over
            }, gameManagerEntityId);
            playerIds.Clear();
            startedGame = false;
            sentStartBroadcast = false;
        }
    }
}