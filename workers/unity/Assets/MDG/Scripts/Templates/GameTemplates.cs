﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Lobby;
using Improbable.Gdk.Core;
using Improbable;
using ResourceSchema = MdgSchema.Game.Resource;
using GameSchema = MdgSchema.Game;
using MDG.ScriptableObjects.Game;

namespace MDG.Templates
{
    public class GameTemplates
    {
        public static EntityTemplate CreateGameManagerTemplate(GameConfig gameConfig)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot("GameManager"), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Position.Snapshot(), UnityGameLogicConnector.WorkerType); 
            template.AddComponent(new Persistence.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new GameSchema.GameStatus.Snapshot
            {
                TimeLeft = gameConfig.GameTime
            }, UnityGameLogicConnector.WorkerType);
            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);

            return template;
        }

        public static EntityTemplate CreateResourceEntityTemplate()
        {
            var template = new EntityTemplate();
            template.AddComponent(new Metadata.Snapshot("Resource"), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Position.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Persistence.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new ResourceSchema.ResourceMetadata.Snapshot
            {
                MaximumOccupancy = 1,
                ResourceType = ResourceSchema.ResourceType.MINERAL
            }, UnityGameLogicConnector.WorkerType);


            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);

            return template;
        }
    }
}