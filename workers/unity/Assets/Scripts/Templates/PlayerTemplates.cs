using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using MdgSchema.Player;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using MdgSchema.Common;
using PositionSchema = MdgSchema.Common.Position;
using InventorySchema = MdgSchema.Common.Inventory;
using CollisionSchema = MdgSchema.Common.Collision;
using StatSchema = MdgSchema.Common.Stats;
using PointSchema = MdgSchema.Common.Point;
using SpawnSchema = MdgSchema.Common.Spawn;
using Unity.Entities;
using MDG.Invader.Components;
using MDG.Common;
using MDG.Common.Components;

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
            if (playerCreationArguments.Length > 0)
            {
                DTO.PlayerConfig creationArgs = DTO.Converters.DeserializeArguments<DTO.PlayerConfig>(playerCreationArguments);
                // GOtta rethink where I'll store usernames and such.
                template.AddComponent(new PlayerMetaData.Snapshot("username"), clientAttribute);
                template.AddComponent(new GameMetadata.Snapshot
                {
                    Type = creationArgs.playerType
                }, serverAttribute);
                template = creationArgs.playerType == GameEntityTypes.Hunter ? 
                        AddInvaderComponents(clientAttribute,template) 
                    : AddDefenderComponents(clientAttribute, template);
                Debug.Log("Creation args position " + creationArgs.position);
                template.AddComponent(new EntityTransform.Snapshot
                {
                    Position = creationArgs.position
                }, serverAttribute);

                template.AddComponent(new Position.Snapshot
                {
                    Coords = new Coordinates(creationArgs.position.X, creationArgs.position.Y, creationArgs.position.Z)
                }, serverAttribute);
            }


            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
           
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
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

            Debug.LogError("Adding defender components");
            template.AddComponent(new StatSchema.MovementSpeed.Snapshot
            {
                LinearSpeed = 100.0f,
                AngularSpeed = 10.0f
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Position = new Vector3f(0, 0, 0),
                Dimensions = new Vector3f(30, 0, 30)
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, serverAttribute);


            template.AddComponent(new SpawnSchema.RespawnMetadata.Snapshot
            {
                BaseRespawnPosition = Vector3f.Zero,
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
                Velocity = Vector3f.Zero
            }, clientAttribute);

            template.AddComponent(new PositionSchema.AngularVelocity.Snapshot
            {
                AngularVelocity = Vector3f.Zero
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
        public static void AddDefenderArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            if (!authoritative)
            {
                Debug.Log("Adding enemy component to defender");
                entityManager.AddComponent<Enemy>(entity);
                entityManager.AddComponent<Clickable>(entity);
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