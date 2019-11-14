using Improbable;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using System.Collections;
using System.Collections.Generic;

using GameSchema = MdgSchema.Common;
namespace MDG.Templates
{
    public class CommonTemplates
    {
        public static void AddRequiredSpatialComponents(EntityTemplate template, string metadata)
        {
            template.AddComponent(new Metadata.Snapshot(metadata), UnityGameLogicConnector.WorkerType);
            template.AddComponent(new Position.Snapshot(), UnityGameLogicConnector.WorkerType);
            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, UnityGameLogicConnector.WorkerType);
        }

        public static void AddRequiredGameEntityComponents(EntityTemplate template, Vector3f initialPosition,
            GameSchema.GameEntityTypes gameEntityType, int typeId = 1)
        {
            template.AddComponent(new EntityTransform.Snapshot
            {
                Position = initialPosition
            }, UnityGameLogicConnector.WorkerType);

            template.AddComponent(new GameSchema.GameMetadata.Snapshot
            {
                Type = gameEntityType,
                TypeId = typeId
            }, UnityGameLogicConnector.WorkerType);
        }
    }
}