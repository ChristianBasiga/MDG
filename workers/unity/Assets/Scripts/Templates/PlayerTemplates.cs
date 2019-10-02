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
using InventorySchema = MdgSchema.Common.Inventory;
using PointSchema = MdgSchema.Common.Point;

namespace MDG.Player
{
    public class Templates
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
                template = creationArgs.playerType == GameEntityTypes.Hunter ? AddHunterComponents(template) : AddHunterComponents(template);
            }
           
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            // No need for player transform, enttiy transform, etc. is enough now.
            template.AddComponent(new PlayerTransform.Snapshot(), clientAttribute);
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);
            return template;
        }

        private static EntityTemplate AddHunterComponents(EntityTemplate template)
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

        private static EntityTemplate AddDefenderComponents(EntityTemplate template)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

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

            return template;
        }
             


    }
}