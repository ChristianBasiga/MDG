using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using MDG.Common;
using MDG.Common.Components;
using MdgSchema.Common;
using MdgSchema.Player;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using CollisionSchema = MdgSchema.Common.Collision;
using InventorySchema = MdgSchema.Common.Inventory;
using PointSchema = MdgSchema.Common.Point;
using PositionSchema = MdgSchema.Common.Position;
using SpawnSchema = MdgSchema.Common.Spawn;
using StatSchema = MdgSchema.Common.Stats;

namespace MDG.Templates
{
    public class PlayerTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] playerCreationArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            //Deserializate playerCreationArguments.
            var template = new EntityTemplate();

            template.AddComponent(new EntityRotation.Snapshot
            {
                Rotation = new MdgSchema.Common.Util.Vector3f(0, 0, 0)
            }, clientAttribute);

            DTO.PlayerConfig creationArgs = DTO.Converters.DeserializeArguments<DTO.PlayerConfig>(playerCreationArguments);
            // GOtta rethink where I'll store usernames and such.
            template.AddComponent(new PlayerMetaData.Snapshot("username"), clientAttribute);
            template.AddComponent(new GameMetadata.Snapshot
            {
                Type = creationArgs.PlayerType
            }, serverAttribute);
            template = creationArgs.PlayerType == GameEntityTypes.Invader ?
                    AddInvaderComponents(clientAttribute, template)
                : AddDefenderComponents(clientAttribute, template);
            template.AddComponent(new EntityPosition.Snapshot
            {
                Position = creationArgs.Position
            }, serverAttribute);

            template.AddComponent(new Position.Snapshot
            {
                Coords = new Coordinates(creationArgs.Position.X, creationArgs.Position.Y, creationArgs.Position.Z)
            }, serverAttribute);


            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);
            return template;
        }

        private static EntityTemplate AddInvaderComponents(string clientAttribute, EntityTemplate template)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new PointSchema.PointMetadata.Snapshot
            {
                IdleGainRate = 1,
                StartingPoints = 1000
            }, serverAttribute);

            template.AddComponent(new PointSchema.Point.Snapshot
            {
                Value = 1000
            }, serverAttribute);

            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                Inventory = new Dictionary<int, InventorySchema.Item>(),
                InventorySize = 5
            }, serverAttribute);

            return template;
        }

        private static EntityTemplate AddDefenderComponents(string clientAttribute, EntityTemplate template)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new StatSchema.MovementSpeed.Snapshot
            {
                LinearSpeed = 100.0f,
                AngularSpeed = 10.0f
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Position =  new MdgSchema.Common.Util.Vector3f(0, 0, 0),
                Dimensions = new MdgSchema.Common.Util.Vector3f(15, 0, 15),
                IsTrigger = false
            }, serverAttribute);


            // Changing from server to client attribute due to nature of how I'm handling collisions now. Low key dangeorus.
            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, clientAttribute);


            template.AddComponent(new SpawnSchema.RespawnMetadata.Snapshot
            {
                BaseRespawnPosition =  new MdgSchema.Common.Util.Vector3f(0, 0, 0),
                BaseRespawnTime = 5.0f,
            }, serverAttribute);

            template.AddComponent(new SpawnSchema.PendingRespawn.Snapshot
            {
                RespawnActive = false,
            }, serverAttribute);

            template.AddComponent(new PointSchema.PointMetadata.Snapshot
            {
                IdleGainRate = 10,
                StartingPoints = 1500
            }, serverAttribute);

            template.AddComponent(new PointSchema.Point.Snapshot
            {
                Value = 1500
            }, serverAttribute);

            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                Inventory = new Dictionary<int, InventorySchema.Item>(),
                InventorySize = 10
            }, serverAttribute);

            template.AddComponent(new PositionSchema.LinearVelocity.Snapshot
            {
                Velocity = new MdgSchema.Common.Util.Vector3f(0,0,0)
            }, clientAttribute);

            template.AddComponent(new PositionSchema.AngularVelocity.Snapshot
            {
                AngularVelocity = new MdgSchema.Common.Util.Vector3f(0, 0, 0)
            }, clientAttribute);


            template.AddComponent(new StatSchema.StatsMetadata.Snapshot
            {
                Health = 10
            }, serverAttribute);

            template.AddComponent(new StatSchema.Stats.Snapshot
            {
                Health = 10
            }, serverAttribute);
        
            return template;
        }
    }

    public class PlayerArchtypes
    {
        public static void AddInvaderArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            
        }
        public static void AddDefenderArchtype(EntityManager entityManager, Entity entity, bool authoritative, bool isAlly = false)
        {
            if (!authoritative)
            {
                if (!isAlly)
                {
                    // MAybe instead of creation stage, once al players gathered load bit more
                    // and that is when I add these extra components appropriately.
                    // prob cleaner that way
                    Debug.Log("Adding enemy component to defender");
                    entityManager.AddComponent<Enemy>(entity);
                    entityManager.AddComponent<Clickable>(entity);
                }
            }
            else
            {
                entityManager.AddComponentData(entity, new CombatMetadata
                {
                    attackCooldown = 2.0f
                });

                entityManager.AddComponentData(entity, new CombatStats
                {
                    attackCooldown = 0
                });
            }
        }
    }
}