using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;
using Snapshot = Improbable.Gdk.Core.Snapshot;

namespace MDG.Editor
{
    internal static class SnapshotGenerator
    {
        public struct Arguments
        {
            public string OutputPath;
        }

        public static void Generate(Arguments arguments)
        {
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot();

            Debug.Log($"Writing snapshot to: {arguments.OutputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }

        private static Snapshot CreateSnapshot()
        {
            var snapshot = new Snapshot();

            AddPlayerSpawner(snapshot);
            AddSpawnManager(snapshot);
            AddGameManager(snapshot);
            // If not do game launcher here, ma need to store lobby as scene in game.
            //down the line.
            // AddLobby(snapshot);
            return snapshot;
        }

        private static void AddGameManager(Snapshot snapshot)
        {
            // Okayyy, so.. Snapshot and game objects don't get along.
            snapshot.AddEntity(Templates.GameTemplates.CreateGameManagerTemplate());
        }

        private static void AddSpawnManager(Snapshot snapshot)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            EntityTemplate template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "SpawnManager" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new MdgSchema.Common.Spawn.SpawnManager.Snapshot(), serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
        private static void AddResources(Snapshot snapshot)
        {
            snapshot.AddEntity(MDG.Common.Templates.GetResourceTemplate());
        }

        private static void AddLobby(Snapshot snapshot)
        {
            snapshot.AddEntity(Lobby.Templates.CreateLobbyTemplate());
        }

        private static void AddPlayerSpawner(Snapshot snapshot)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new PlayerCreator.Snapshot(), serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
    }
}
