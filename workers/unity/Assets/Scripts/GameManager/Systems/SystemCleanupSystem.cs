using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using GameSchema = MdgSchema.Game;

// This has to be on client and server. And systems to remove is from external source depending on that upon connecting.
// So probably OnConnectionEstablished.
namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(Point.PointSystem))]
    [UpdateAfter(typeof(Position.PositionSystem))]
    [UpdateAfter(typeof(Spawn.RespawnMonitorSystem))]
    public class SystemCleanupSystem : ComponentSystem
    {
        ComponentUpdateSystem componentUpdateSystem;
        List<ComponentSystemBase> systemsToRemove;
        protected override void OnCreate()
        {
            base.OnCreate();
            systemsToRemove = new List<ComponentSystemBase>();
            systemsToRemove.Add(World.GetExistingSystem<Point.PointSystem>());
            systemsToRemove.Add(World.GetExistingSystem<Stat.StatMonitorSystem>());
            systemsToRemove.Add(World.GetExistingSystem<Spawn.RespawnMonitorSystem>());
            systemsToRemove.Add(World.GetExistingSystem<Collision.CollisionDetectionSystem>());
            systemsToRemove.Add(World.GetExistingSystem<Collision.CollisionHandlerSystem>());
            systemsToRemove.Add(World.GetExistingSystem<Position.PositionSystem>());

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            // Polish this furher later.

            var endGameEventMessages = componentUpdateSystem.GetEventsReceived<GameSchema.GameStatus.EndGame.Event>();
            if (endGameEventMessages.Count > 0)
            {
                // Prob transition to new scene, or straight up just close it.
                foreach (ComponentSystemBase componentSystemBase in systemsToRemove)
                {
                    try
                    {
                        World.DestroySystem(componentSystemBase);
                    }
                    catch(System.Exception err)
                    {
                        UnityEngine.Debug.Log($"Failed to delete system {componentSystemBase.GetType()} {err}");
                    }
                }

                World.DestroySystem(this);
            }
        }
    }
}