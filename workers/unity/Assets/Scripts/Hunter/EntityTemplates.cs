using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using MdgSchema.Common;
namespace MDG.Hunter.Unit
{
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

        public static EntityTemplate GetUnitEntityTemplate(string workerId)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            EntityTemplate template = new EntityTemplate();
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            //Create system which acts upon this.
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Unit" }, serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot { Type = GameEntityTypes.Unit }, serverAttribute);
            template.AddComponent(new EntityTransform.Snapshot(), clientAttribute);
            template.AddComponent(new EntityCollider.Snapshot(), serverAttribute);
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            // Must have read access from both sides.
            template.SetReadAccess(clientAttribute, UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);

            return template;
        }
        public static EntityTemplate GetCollectorUnitEntityTemplate(string workerType)
        {
            EntityTemplate template = GetUnitEntityTemplate(workerType);
            //Add Collect specific components here such as inventory and health.
            return template;
        }
    }
}