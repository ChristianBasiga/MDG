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
        // If can't reproduce it like this, look at entity creation again.
        //But worst case I simply add the lobby components to respective player if authoritiave client
        //so that they may send commands.
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] playerCreationArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            //Deserializate playerCreationArguments.
            var template = new EntityTemplate();


            // Tod, let's not have these repeated checks. Instead store snapshots.
            if (playerCreationArguments.Length > 0)
            {
                DTO.PlayerConfig creationArgs = DTO.Converters.DeserializeArguments<DTO.PlayerConfig>(playerCreationArguments);
                //The creation args would come from room clear request / start game request.
                // GOtta rethink where I'll store usernames and such.
                template.AddComponent(new PlayerMetaData.Snapshot("username"), clientAttribute);
                template.AddComponent(new GameMetadata.Snapshot
                {
                    Type = creationArgs.playerType
                }, serverAttribute);
                //Factory instead incase of other roles down the line.
                template = creationArgs.playerType == GameEntityTypes.Hunter ? AddHunterComponents(template) : AddHunterComponents(template);
                template.AddComponent(new InventorySchema.Inventory.Snapshot
                {
                    Inventory = new Dictionary<int, InventorySchema.Item>(),
                    InventorySize = (uint)(creationArgs.playerType == GameEntityTypes.Hunter ? 5 : 10)
                }, serverAttribute);

                template.AddComponent(new PointSchema.PointMetadata.Snapshot
                {
                    IdleGainRate = creationArgs.playerType == GameEntityTypes.Hunter ? 1 : 10,
                    StartingPoints = creationArgs.playerType == GameEntityTypes.Hunter ? 1000 : 1500
                }, serverAttribute);
                template.AddComponent(new PointSchema.Point.Snapshot
                {
                    Value = creationArgs.playerType == GameEntityTypes.Hunter ? 1000 : 1500
                },serverAttribute);
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
            return template;
        }

        private static EntityTemplate AddHuntedComponents(EntityTemplate template)
        {
            return template;
        }
             


    }
}