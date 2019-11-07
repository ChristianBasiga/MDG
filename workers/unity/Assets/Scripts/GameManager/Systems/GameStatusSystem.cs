using Improbable.Gdk.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using GameSchema = MdgSchema.Game;
namespace MDG.Game
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class GameStatusSystem : ComponentSystem
    {

        CommandSystem commandSystem;
        WorkerSystem workerSystem;
        EntityQuery gameStatusQuery;
        bool startedGame = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            gameStatusQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<GameSchema.GameStatus.Component>(),
                ComponentType.ReadOnly<GameSchema.GameStatus.ComponentAuthority>()
                );
            gameStatusQuery.SetFilter(GameSchema.GameStatus.ComponentAuthority.Authoritative);
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override void OnUpdate()
        {

            var startGameRequests = commandSystem.GetRequests<GameSchema.GameStatus.StartGame.ReceivedRequest>(new EntityId(3));

            if (!startedGame && startGameRequests.Count > 0)
            {
                startedGame = true;     
                workerSystem.TryGetEntity(new EntityId(3), out Unity.Entities.Entity gameManagerEntity);
                // Strangely enough, snapshot setting this alo to 900 not working. Hmm
                EntityManager.SetComponentData(gameManagerEntity, new GameSchema.GameStatus.Component
                {
                    TimeLeft = 900.0f
                });
                return;
            }


            if (startedGame)
            {
                Entities.With(gameStatusQuery).ForEach((ref SpatialEntityId spatialEntityId, ref GameSchema.GameStatus.Component gameStatus) =>
                {
                    if (gameStatus.TimeLeft > 0)
                    {
                        gameStatus.TimeLeft = max(gameStatus.TimeLeft - UnityEngine.Time.deltaTime, 0);
                    }
                });
            }
        }
    }
}