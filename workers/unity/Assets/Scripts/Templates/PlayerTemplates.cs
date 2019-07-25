using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using MdgSchema.Player;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;

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

            if (playerCreationArguments.Length > 0)
            {
                DTO.PlayerConfig creationArgs = DTO.Converters.DeserializeArguments<DTO.PlayerConfig>(playerCreationArguments);
                //The creation args would come from room clear request / start game request.
                template.AddComponent(new PlayerMetaData.Snapshot("username",creationArgs.playerType), clientAttribute);
            }
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new PlayerTransform.Snapshot(), clientAttribute);
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);
            return template;
        }

    }
}