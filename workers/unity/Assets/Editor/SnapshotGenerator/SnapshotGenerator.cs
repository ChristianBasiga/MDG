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

           // AddResourceManager(snapshot);
            AddPlayerSpawner(snapshot);
            // AddLobby(snapshot);
            //AddUnitSpawner(snapshot);
            return snapshot;
        }

        private static void AddResourceManager(Snapshot snapshot)
        {
            snapshot.AddEntity(MDG.Common.Templates.GetResourceManagerTemplate());
        }
        // Will load resources on client connect, but can't be part of snapshot due to list of occupants.
        // Maybe create component like Occupyiable? lol. I could.
        // Cause resources are inheritently part of snapshot and should be.
        private static void AddResources(Snapshot snapshot)
        {
            snapshot.AddEntity(MDG.Common.Templates.GetResourceTemplate());
        }

        // Should also add GameManager
        private static void AddUnitSpawner(Snapshot snapshot)
        {
            snapshot.AddEntity(MDG.Hunter.Unit.Templates.GetUnitSpawnerTemplate());
        }

        private static void AddLobby(Snapshot snapshot)
        {
            snapshot.AddEntity(Lobby.Templates.CreateLobbyTemplate());
        }

        private static void AddPlayerSpawner(Snapshot snapshot)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            var template = new EntityTemplate();
            Debug.Log(Position.ComponentId);
            // Figure out why this is fucked up.
            Debug.Log(Metadata.ComponentId);
            Debug.Log(Persistence.ComponentId);
            Debug.Log(PlayerCreator.ComponentId);
            Debug.Log(EntityAcl.ComponentId);
            template.AddComponent(new Position.Snapshot(), serverAttribute);
           // template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, serverAttribute);
       /*     template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new PlayerCreator.Snapshot(), serverAttribute);
            */
            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, MobileClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
    }
}
