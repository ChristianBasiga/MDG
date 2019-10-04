using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using MdgSchema.Common;
using MdgSchema.Units;
using MdgSchema.Common.Point;
using MdgSchema.Game.Resource;
namespace MDG.Common
{
    public class Templates
    {
        // For now basic functionality it what I want. Worry about granular scheme of how I'll handle resources.
        // This first sprint is the true POC I should have had. Then build it greater.
        public static EntityTemplate GetResourceTemplate()
        {
            // Replace this later with server worker that manges resource management.
            const string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            // Actually, hmm if server attribute only one that can write to resource component.
            // then I can't do it in CommandUpdateSystem.
            template.AddComponent(new EntityTransform.Snapshot(), serverAttribute);
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Resource" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new ResourceMetadata.Snapshot {
                MaximumOccupancy = 10,
                Health = 10,
                ResourceType = ResourceType.MINERAL
            }, serverAttribute);
            template.AddComponent(new Resource.Snapshot
            {
                Health = 10,
                Occupants = new List<EntityId>(),
            }, serverAttribute);
            // Should be pointvalue component instead, but reusing this is fine, HONESTLY.
            template.AddComponent(new PointMetadata.Snapshot
            {
                IdleGainRate = 0,
                StartingPoints = 10
            }, serverAttribute);
           
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }

        // Insted of snapshot create entity in server world upon creation.
        public static EntityTemplate GetResourceManagerTemplate()
        {
            const string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "ResourceManager" }, serverAttribute);
            template.AddComponent(new ResourceManager.Snapshot(), serverAttribute);
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);
            return template;
        }
    }
}