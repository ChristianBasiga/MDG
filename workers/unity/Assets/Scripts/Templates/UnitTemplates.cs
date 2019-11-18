using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using MdgSchema.Common;
using InventorySchema = MdgSchema.Common.Inventory;
using PointSchema = MdgSchema.Common.Point;
using UnitsSchema = MdgSchema.Units;
using UnitComponents = MDG.Invader.Components;
using Unity.Entities;
using MDG.Common.Components;
using MDG.Invader.Components;
using MdgSchema.Units;
using CollisionSchema = MdgSchema.Common.Collision;
using StatSchema = MdgSchema.Common.Stats;
using MdgSchema.Common.Position;
using MDG.DTO;
using SpawnSchema = MdgSchema.Common.Spawn;
using MDG.Common;

namespace MDG.Templates
{
    public class UnitTemplates
    {
        // Using serialized args, I COULD make an interface / base function they all call that then calls specific one
        // based on game type. That would be 100% better, but that's elegance.
        public static EntityTemplate GetUnitEntityTemplate(string workerId, UnitTypes unitType, Vector3f spawnPositon, byte[] spawnArgs = null)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            template.AddComponent(new EntityTransform.Snapshot { Position = spawnPositon }, serverAttribute);
            template.AddComponent(new LinearVelocity.Snapshot { Velocity = Vector3f.Zero }, clientAttribute);
            template.AddComponent(new AngularVelocity.Snapshot { AngularVelocity = Vector3f.Zero }, clientAttribute);

            template.AddComponent(new PointSchema.Point.Snapshot
            {
                Value = 10000
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Position = new Vector3f(0,0,0),
                Dimensions = new Vector3f(15, 0, 15)
            }, serverAttribute);

            UnitsSchema.Unit.Snapshot unitSnapshot = new Unit.Snapshot { Type = unitType };
            if (spawnArgs != null)
            {
                UnitConfig unitConfig = Converters.DeserializeArguments<UnitConfig>(spawnArgs);
                Debug.Log("Unit config has owner id " + unitConfig.ownerId);
                unitSnapshot.OwnerId = new EntityId(unitConfig.ownerId);
                unitSnapshot.Type = unitConfig.unitType;
            }
            template.AddComponent(unitSnapshot, serverAttribute);

            switch (unitType)
            {
                case UnitsSchema.UnitTypes.WORKER:
                    MakeWorkerUnit(template, clientAttribute);
                    break;
                case UnitsSchema.UnitTypes.TANK:
                    break;
            }

            template.AddComponent(new SpawnSchema.RespawnMetadata.Snapshot
            {
                BaseRespawnPosition = Vector3f.Zero,
                BaseRespawnTime = 60.0f
            }, serverAttribute);

            template.AddComponent(new SpawnSchema.PendingRespawn.Snapshot
            {
                RespawnActive = false,
            }, serverAttribute);


            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.SetReadAccess(clientAttribute, UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);
            return template;
        }
        /*
        public static EntityTemplate GetUnitEntityTemplate(string workerId, byte[] serializedArgs)
        {

            UnitConfig unitConfig = Converters.DeserializeArguments<UnitConfig>(serializedArgs);
            return GetUnitEntityTemplate(workerId, unitConfig.unitType, unitConfig.spawnPosition);
        }*/


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


            template.AddComponent(new StatSchema.Stats.Snapshot
            {
                Health = 10
            }
            , clientAttribute);

            template.AddComponent(new UnitsSchema.Unit.Snapshot
            {
                Type = UnitsSchema.UnitTypes.TANK
            }, serverAttribute);
        }
    }
    // For adding componetns to entities that don't need to be synced with server.
    // Do this for all entities.
    public class UnitArchtypes
    {
        public static void AddUnitArchtype(EntityManager  entityManager, Entity entity, bool authoritative, UnitsSchema.UnitTypes type)
        {
            if (authoritative)
            {
                entityManager.AddComponentData(entity, new CommandListener { CommandType = MDG.Invader.Commands.CommandType.None });
               
            }
            else
            {
                Debug.Log("Adding enemy component to unit");
                entityManager.AddComponentData(entity, new Enemy());
            }
            entityManager.AddComponent<Clickable>(entity);

            switch (type)
            {
                case UnitTypes.WORKER:
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
            }
        }
    }
}