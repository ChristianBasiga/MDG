using Improbable;
using Improbable.Gdk.Core;
using MDG.Common;
using MDG.Common.Components;
using MDG.DTO;
using MDG.Invader.Components;
using MdgSchema.Common;
using MdgSchema.Common.Position;
using MdgSchema.Common.Util;
using MdgSchema.Units;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using CollisionSchema = MdgSchema.Common.Collision;
using InventorySchema = MdgSchema.Common.Inventory;
using PointSchema = MdgSchema.Common.Point;
using StatSchema = MdgSchema.Common.Stats;
using UnitsSchema = MdgSchema.Units;

namespace MDG.Templates
{
    public class UnitTemplates
    {
        public static EntityTemplate GetUnitEntityTemplate(string workerId, Vector3f spawnPositon, byte[] spawnArgs = null)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();

            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            template.AddComponent(new EntityPosition.Snapshot { Position = spawnPositon }, serverAttribute);
            template.AddComponent(new EntityRotation.Snapshot { Rotation = new Vector3f(0, 0, 0) }, clientAttribute);
            template.AddComponent(new LinearVelocity.Snapshot { Velocity = new Vector3f(0,0,0) }, clientAttribute);
            template.AddComponent(new AngularVelocity.Snapshot { AngularVelocity = new Vector3f(0,0,0) }, clientAttribute);



            template.AddComponent(new PointSchema.Point.Snapshot
            {
                Value = 10000
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, clientAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Position = new Vector3f(0,0,0),
                Dimensions = new Vector3f(30, 0, 30)
            }, serverAttribute);

            UnitConfig unitConfig = Converters.DeserializeArguments<UnitConfig>(spawnArgs);
            template.AddComponent(new Unit.Snapshot
            {
                OwnerId = new EntityId(unitConfig.OwnerId),
                Type = unitConfig.UnitType
            }, serverAttribute);

            template.AddComponent(new Owner.Snapshot
            {
                OwnerId = new EntityId(unitConfig.OwnerId)
            }, serverAttribute);

            template.AddComponent(new StatSchema.MovementSpeed.Snapshot
            {
                LinearSpeed = 100.0f,
                AngularSpeed = 10.0f
            }, serverAttribute);

            switch (unitConfig.UnitType)
            {
                case UnitsSchema.UnitTypes.Worker:
                    MakeWorkerUnit(template, clientAttribute);
                    break;
                case UnitsSchema.UnitTypes.Tank:
                    break;
                default:
                    throw new System.Exception("Not Suppored Unit Type");
            }
            template.AddComponent(new Position.Snapshot
            {
                Coords = new Coordinates(spawnPositon.X, spawnPositon.Y, spawnPositon.Z)
            }, serverAttribute);
            template.SetReadAccess(clientAttribute, UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);
            return template;
        }

        private static void MakeWorkerUnit(EntityTemplate template, string clientAttribute)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                Inventory = new Dictionary<int, InventorySchema.Item>(),
                InventorySize = 6
            }, serverAttribute);

            template.AddComponent(new StatSchema.StatsMetadata.Snapshot
            {
                Health = 5
            }, serverAttribute);

            template.AddComponent(new StatSchema.Stats.Snapshot {
                Health = 5
            }
            , serverAttribute);
        }

        private static void MakeTankUnit(EntityTemplate template, string clientAttribute)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new StatSchema.StatsMetadata.Snapshot
            {
                Health = 5
            }, serverAttribute);

            template.AddComponent(new StatSchema.Stats.Snapshot
            {
                Health = 10
            }
            , clientAttribute);

            template.AddComponent(new UnitsSchema.Unit.Snapshot
            {
                Type = UnitsSchema.UnitTypes.Tank
            }, serverAttribute);
        }
    }
    // For adding componetns to entities that don't need to be synced with server.
    // Do this for all entities.
    public class UnitArchtypes
    {
        public static void AddUnitArchtype(EntityManager  entityManager, Entity entity, bool authoritative, UnitsSchema.UnitTypes type)
        {
            // Either have this life cycle check on each weapon which I do have
            // but that prob slows things down. It should be authority upon player not
            // each entit
            if (authoritative)
            {
                entityManager.AddComponentData(entity, new CommandListener { CommandType = CommandType.None });
            }
            else
            {
                Debug.Log("Adding enemy component to unit");
                entityManager.AddComponentData(entity, new Enemy());
            }
            entityManager.AddComponent<Clickable>(entity);

            switch (type)
            {
                case UnitTypes.Worker:
                    AddWorkerUnitArchtype(entityManager, entity, authoritative);
                    break;
            }
        }

        private static void AddWorkerUnitArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            // Replace all hard coded values with scriptab le objects.
            if (authoritative)
            {
                entityManager.AddComponentData(entity, new CombatMetadata
                {
                    attackCooldown = 2.0f,
                    attackRange = 90.0f
                });

                entityManager.AddComponentData(entity, new CombatStats
                {
                    attackCooldown = 0,
                    attackRange = 90.0f
                });

                entityManager.AddComponentData(entity, new WorkerUnit());
            }
        }
    }
}