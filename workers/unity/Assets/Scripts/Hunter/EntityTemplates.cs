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
namespace MDG.Hunter.Unit
{
    // So all templates must have dictionary
    public class Templates
    {
        public static EntityTemplate GetUnitSpawnerTemplate()
        {
            EntityTemplate template = new EntityTemplate();

            var serverAttribute = UnityGameLogicConnector.WorkerType;

            template.AddComponent(new Metadata.Snapshot { EntityType = "UnitSpawner" }, serverAttribute);
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new MdgSchema.Spawners.UnitSpawner.Snapshot(), serverAttribute);
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;

        }

        // Then typeId gets parsed to unit type.
        public static EntityTemplate GetUnitEntityTemplate(string workerId, int typeId = 0)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            EntityTemplate template = new EntityTemplate();
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            //Create system which acts upon this.
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            template.AddComponent(new EntityTransform.Snapshot { Scale = new Vector3f(10,10,10)}, clientAttribute);
            template.AddComponent(new Stats.Snapshot{ Health = 5}, clientAttribute);
            template.AddComponent(new InventorySchema.Inventory.Snapshot {
                Inventory = new Dictionary<int, InventorySchema.Item>(),
                InventorySize = 6
            }, serverAttribute);
            // Actuall this is collider on entity, so position will always be unit position
            // prob shouldn't track this here.
            template.AddComponent(new EntityCollider.Snapshot {
                Radius = 5.0f,
                ColliderType = ColliderType.SPHERE
            }, serverAttribute);

            template.AddComponent(new UnitsSchema.Unit.Snapshot {
                Type = (UnitsSchema.UnitTypes)typeId
            }, clientAttribute);

            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.SetReadAccess(clientAttribute, UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);
            return template;
        }
        public static EntityTemplate GetCollectorUnitEntityTemplate(string workerType)
        {
            EntityTemplate template = GetUnitEntityTemplate(workerType);
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerType);

            template.AddComponent(new MdgSchema.Units.Unit.Snapshot
            {
                Type = UnitsSchema.UnitTypes.COLLECTOR
            }, clientAttribute);
            //Add Collect specific components here such as inventory and health.
            return template;
        }
    }
}