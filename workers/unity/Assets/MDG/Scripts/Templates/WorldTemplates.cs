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
using MdgSchema.Common.Spawn;
using CollisionSchema = MdgSchema.Common.Collision;
using TerritorySchema = MdgSchema.Game.Territory;
using MdgSchema.Common.Util;
using WorldObjects = MDG.ScriptableObjects.World;
using MDG.Common;
using Improbable.Gdk.QueryBasedInterest;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Templates
{
    public class WorldTemplates
    {

        public static EntityTemplate GetResourceTemplate()
        {
            // Replace this later with server worker that manges resource management.
            const string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate template = new EntityTemplate();
            // Actually, hmm if server attribute only one that can write to resource component.
            // then I can't do it in CommandUpdateSystem.
            template.AddComponent(new EntityPosition.Snapshot(), serverAttribute);
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new GameMetadata.Snapshot
            {
                Type = GameEntityTypes.Resource,
                TypeId = 0,
            }, serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Resource" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            
            template.AddComponent(new ResourceMetadata.Snapshot {
                MaximumOccupancy = 10,
                Health = 10,
                ResourceType = ResourceType.MINERAL,
                RespawnTime = 10.0f,
                WillRespawn = true
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

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Dimensions = new Vector3f(10, 0, 10),
                IsTrigger = false,
                Position = new Vector3f(0,0,0)
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                IsTrigger = true
            }, serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }

        public static EntityTemplate GetTerritoryTemplate(WorldObjects.Territory territory)
        {
            EntityTemplate template = new EntityTemplate();
            string serverAttribute = UnityGameLogicConnector.WorkerType;

            CommonTemplates.AddRequiredSpatialComponents(template, "Territory");
            CommonTemplates.AddRequiredGameEntityComponents(template, HelperFunctions.Vector3fFromUnityVector(territory.Position), GameEntityTypes.Territory);

            template.AddComponent(new EntityRotation.Snapshot
            {
                Rotation = new Vector3f(0, 0, 0)
            }, serverAttribute);

            template.SetComponent(new Position.Snapshot
            {
                Coords = HelperFunctions.CoordinatesFromUnityVector(territory.Position)
            });
            template.AddComponent(new TerritorySchema.Territory.Snapshot
            {
                PointGain = territory.PointGain,
                TerritoryId = territory.Name,
                ParticipationRadius = territory.ParticipationRadius,
                TimeToClaim = territory.ClaimTime
            }, serverAttribute);

            template.AddComponent(new TerritorySchema.TerritoryStatus.Snapshot
            {
                Status = TerritorySchema.TerritoryStatusTypes.Released
            }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            return template;
        }
    }
}