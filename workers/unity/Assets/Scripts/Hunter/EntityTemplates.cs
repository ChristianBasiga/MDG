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
using UnitComponents = MDG.Hunter.Components;
using Unity.Entities;
using MDG.Common.Components;
using MDG.Hunter.Components;

namespace MDG.Hunter.Unit
{
    public class Templates
    {
        public static EntityTemplate GetUnitEntityTemplate(string workerId, int typeId = 1)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            
            // Actuall this is collider on entity, so position will always be unit position
            // prob shouldn't track this here.
            template.AddComponent(new EntityCollider.Snapshot {
                Radius = 5.0f,
                ColliderType = ColliderType.SPHERE
            }, serverAttribute);

            UnitsSchema.UnitTypes unitType = (UnitsSchema.UnitTypes)typeId;
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


        private static void MakeWorkerUnit(EntityTemplate template, string clientAttribute)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new UnitsSchema.Unit.Snapshot
            {
                Type = UnitsSchema.UnitTypes.WORKER
            }, serverAttribute);
            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                Inventory = new Dictionary<int, InventorySchema.Item>(),
                InventorySize = 6
            }, serverAttribute);
            template.AddComponent(new EntityTransform.Snapshot { Scale = new Vector3f(10, 10, 10) }, clientAttribute);

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

            // Scale is REALLY Irrelevant tbh. transform should just e position at this point.
            template.AddComponent(new EntityTransform.Snapshot {
                Scale = new Vector3f(10, 10, 10)
            }, 
            clientAttribute);
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
                entityManager.AddComponentData(entity, new CommandListener { CommandType = Commands.CommandType.None });
            }
            entityManager.AddComponent<Clickable>(entity);
        }

        public static void AddWorkerUnitArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {

        }
    }
}