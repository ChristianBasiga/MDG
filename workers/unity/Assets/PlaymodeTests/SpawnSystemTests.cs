using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using MDG;
using MDG.ClientSide.UserInterface;
using MDG.Common.Components;
using MDG.Hunter.Components;
using MdgSchema.Common;
using MdgSchema.Units;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SpawnSchema = MdgSchema.Common.Spawn;
using SpawnSystems = MDG.Common.Systems.Spawn;
namespace PlaymodeTests
{
    public class SpawnSystemTests: IPrebuildSetup
    {
        LinkedEntityComponent linkedEntityComponent;
        WorkerSystem workerSystem;
        CommandSystem commandSystem;
        SpawnSystems.SpawnRequestSystem spawnReqSystem;
        GameObject clientWorker;
        GameObject serverWorker;

        public void Setup()
        {
            clientWorker = new GameObject("ClientWorker");
            clientWorker.AddComponent<UnityClientConnector>();

            serverWorker = new GameObject("GameLogicWorker");
            serverWorker.AddComponent<UnityGameLogicConnector>();

        }

        [UnityTest, Order(40)]
        public IEnumerator SpawnInvaderTest()
        {
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });
            commandSystem = clientWorker.GetComponent<UnityClientConnector>().Worker.World.GetExistingSystem<CommandSystem>();
            spawnReqSystem = workerInWorld?.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
            EntityId spawnedId = new EntityId(-1);
            spawnReqSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = GameEntityTypes.Hunter
            }, (EntityId id) => {
                spawnedId = id;
            }); 
            yield return new WaitUntil(() => { return spawnedId.IsValid(); });
            Assert.True(GameObject.Find("Hunter_Spawned") != null, "Invader failed to spawn");
            // Need to store this somewhere, here's issue. Non authoritative units were spawned.
            // SpawnRequset system HAS to be on client side period.
            for (int i = 0; i < 6; ++i)
            {
                yield return new WaitForEndOfFrame();
            }
            Assert.True(GameObject.FindGameObjectsWithTag("Unit").Length == 3, "Initial Invader units failed to spawn");
        }

        [UnityTest, Order(41)]
        public IEnumerator SpawnAuthoritativeUnitTest()
        {
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });
            commandSystem = clientWorker.GetComponent<UnityClientConnector>().Worker.World.GetExistingSystem<CommandSystem>();
            workerSystem = clientWorker.GetComponent<UnityClientConnector>().Worker.World.GetExistingSystem<WorkerSystem>();
            spawnReqSystem = workerInWorld?.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
            // Remove this as well. All client side requests should'nt be command requests, it's needless overhead and latency.
            SpawnSchema.SpawnRequest payload = new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Position = new Improbable.Vector3f(1,1,1)
            };
            EntityId unitEntityId = new EntityId(-1);
            spawnReqSystem.RequestSpawn(payload,
                (EntityId id) =>
                {
                    unitEntityId = id;
                }
            );
            yield return new WaitUntil(() => { return unitEntityId.IsValid(); });
            yield return new WaitForEndOfFrame();

            List<GameObject> unitObject = clientWorker.GetComponent<UnityClientConnector>().clientGameObjectCreator.GetLinkedGameObjectById(unitEntityId);
            EntityManager entityManager = workerSystem.EntityManager;
            Assert.IsNotNull(unitObject, $"Linked GameObject not created for Unit with entity id {unitEntityId}");
            Assert.True(unitObject[0].name.Contains("authoritative"), "Non authoritative unit created for authoritative client");
            Assert.True(workerSystem.TryGetEntity(unitEntityId, out Entity entity));
            // This should be stored somewhere instead of repeating, so much required debt.
            ComponentType[] authoritativeComponentTypes = new ComponentType[2]
            {
                ComponentType.ReadWrite<Clickable>(),
                ComponentType.ReadWrite<CommandListener>()
            };
            // This might be excessive.
            foreach (ComponentType componentType in authoritativeComponentTypes)
            {
                Assert.True(entityManager.HasComponent(entity, componentType), "Authoritative unit missing required components");
            }
        }

        [UnityTest, Order(42)]
        public IEnumerator RespawnTest()
        {

            int initialAmountOfUnitsInScene = GameObject.FindGameObjectsWithTag("Unit").Length;
            LinkedEntityComponent linkedUnit = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>();

            Improbable.Vector3f respawnPosition = new Improbable.Vector3f(1, 1, 1);
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });

            workerSystem = clientWorker.GetComponent<UnityClientConnector>().Worker.World.GetExistingSystem<WorkerSystem>();

            if (workerSystem.TryGetEntity(linkedUnit.EntityId, out Entity entity))
            {
                workerSystem.EntityManager.AddComponentData(entity, new SpawnSchema.PendingRespawn.Component
                {
                    PositionToRespawn = respawnPosition,
                    TimeTillRespawn = 5.0f,
                });
                yield return new WaitForSeconds(5.2f);
                yield return new WaitForEndOfFrame();
                int amountOfUnitsInSceneBeforeRespawn = GameObject.FindGameObjectsWithTag("Unit").Length;
                List<GameObject> removedLinkedObject = clientWorker.GetComponent<UnityClientConnector>().clientGameObjectCreator.GetLinkedGameObjectById(linkedUnit.EntityId);
                Assert.Null(removedLinkedObject, "Failed to delete object");
                int amountOfUnitsAfterRespawn = GameObject.FindGameObjectsWithTag("Unit").Length;
                Assert.AreEqual(initialAmountOfUnitsInScene, amountOfUnitsAfterRespawn, "Object not respawned");
            }
        }
    }
}
