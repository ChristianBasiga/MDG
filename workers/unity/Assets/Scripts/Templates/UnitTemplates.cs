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
using UnitsSchema = MdgSchema.Units;
using UnitComponents = MDG.Invader.Components;
using Unity.Entities;
using MDG.Common.Components;
using MDG.Invader.Components;
using MdgSchema.Units;
using CollisionSchema = MdgSchema.Common.Collision;
using MdgSchema.Common.Position;
using MDG.DTO;

namespace MDG.Templates
{
    public class UnitTemplates
    {
        public static EntityTemplate GetUnitEntityTemplate(string workerId, UnitTypes unitType, Vector3f spawnPositon)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            template.AddComponent(new EntityTransform.Snapshot { Position = spawnPositon }, serverAttribute);
            template.AddComponent(new LinearVelocity.Snapshot { Velocity = Vector3f.Zero }, clientAttribute);
            template.AddComponent(new AngularVelocity.Snapshot { AngularVelocity = Vector3f.Zero }, clientAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Position = new Vector3f(0,0,0),
                Dimensions = new Vector3f(15, 0, 15)
            }, serverAttribute);


            template.AddComponent(new UnitsSchema.Unit.Snapshot
            {
                Type = unitType
            }, serverAttribute);
            switch (unitType)
            {
                case UnitsSchema.UnitTypes.WORKER:
                    MakeWorkerUnit(template, clientAttribute);
                    break;
                case UnitsSchema.UnitTypes.TANK:
                    break;
            }

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

            template.AddComponent(new Stats.Snapshot {
                Health = 5
            }
            , clientAttribute);

        }

        private static void MakeTankUnit(EntityTemplate template, string clientAttribute)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new Stats.Snapshot
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
    public class Archtypes
    {
        public static void AddUnitArchtype(EntityManager  entityManager, Entity entity, bool authoritative, UnitsSchema.UnitTypes type)
        {
            if (authoritative)
            {
                entityManager.AddComponentData(entity, new CommandListener { CommandType = MDG.Invader.Commands.CommandType.None });
            }
            entityManager.AddComponent<Clickable>(entity);
        }

        public static void AddWorkerUnitArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {

        }
    }
}