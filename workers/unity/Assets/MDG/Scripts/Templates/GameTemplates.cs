using Improbable;
using Improbable.Gdk.Core;
using MDG.ScriptableObjects.Game;
using GameSchema = MdgSchema.Game;
using ResourceSchema = MdgSchema.Game.Resource;

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