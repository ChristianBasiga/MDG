using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using MDG;
using MDG.ClientSide.UserInterface;
using MDG.Common.Components;
using MDG.Invader.Components;
using MdgSchema.Common;
using MdgSchema.Units;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SpawnSystems = MDG.Common.Systems.Spawn;
using SpawnSchema = MdgSchema.Common.Spawn;
using PointSchema = MdgSchema.Common.Point;
using PointSystems = MDG.Common.Systems.Point;
using DefenderComponents = MDG.Defender.Components;
namespace PlaymodeTests
{
    public class PointSystemTests : IPrebuildSetup
    {
        CommandSystem commandSystem;
        GameObject clientWorker;
        GameObject serverWorker;
        GameObject pointUI;
        public void Setup()
        {
            clientWorker = new GameObject("ClientWorker");
            var clientConnector = clientWorker.AddComponent<UnityClientConnector>();
            serverWorker = new GameObject("GameLogicWorker");
            serverWorker.AddComponent<UnityGameLogicConnector>();
        }

        [UnityTest, Order(60)]
        public IEnumerator InitialSpawnPointsTest()
        {
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });
            SpawnSystems.SpawnRequestSystem spawnReqSystem = workerInWorld?.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
            EntityId spawnedId = new EntityId(-1);
            spawnReqSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = GameEntityTypes.Hunter
            }, (EntityId id) => {
                spawnedId = id;
            });
            yield return new WaitUntil(() => {
                return spawnedId.IsValid();
            });
            WorkerSystem workerSystem = spawnReqSystem.World.GetExistingSystem<WorkerSystem>();
            if (workerSystem.TryGetEntity(spawnedId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;
                PointSchema.PointMetadata.Component pointMetadataComponent = entityManager.GetComponentData<PointSchema.PointMetadata.Component>(entity);
                PointSchema.Point.Component pointComponent = entityManager.GetComponentData<PointSchema.Point.Component>(entity);
                Assert.AreEqual(pointMetadataComponent.StartingPoints + (pointMetadataComponent.IdleGainRate ), pointComponent.Value,"Gain rate not added to points");
            }
        }

        [UnityTest, Order(61)]
        public IEnumerator AddPointsFromRequestTest()
        {
            yield return new WaitForEndOfFrame();
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });
            var linkedEntityComponent = GameObject.Find("Hunter_Spawned").GetComponent<LinkedEntityComponent>();
            WorkerSystem workerSystem = linkedEntityComponent.World.GetExistingSystem<WorkerSystem>();
           

            EntityId hunterEntityId = linkedEntityComponent.EntityId;

            if (workerSystem.TryGetEntity(hunterEntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;
                var initialPoints = entityManager.GetComponentData<PointSchema.Point.Component>(entity);
                PointSchema.PointMetadata.Component pointMetadataComponent = entityManager.GetComponentData<PointSchema.PointMetadata.Component>(entity);
                PointSystems.PointRequestSystem pointRequestSystem = workerSystem.World.GetExistingSystem<PointSystems.PointRequestSystem>();

                PointSchema.PointRequest pointRequest = new PointSchema.PointRequest
                {
                    EntityUpdating = hunterEntityId,
                    PointUpdate = 100
                };
                PointSchema.PointResponse? response = null;
                int framesPassed = -1;
                pointRequestSystem.AddPointRequest(pointRequest, (PointSchema.PointResponse pointResponse) =>
                {
                    response = pointResponse;
                });
                yield return new WaitWhile(() => {
                    // I guess this isn't ran every frame.
                    framesPassed += 1;
                    return !response.HasValue;
                });

                var updatedPoints = entityManager.GetComponentData<PointSchema.Point.Component>(entity);
                var expectedUpdate = initialPoints.Value + (pointMetadataComponent.IdleGainRate * framesPassed) + pointRequest.PointUpdate;
                Assert.AreEqual( pointRequest.PointUpdate, response.GetValueOrDefault().TotalPoints, "Expected points not matching response");
                Assert.AreEqual(expectedUpdate, updatedPoints.Value, "Points not added correctly");
            }

        }

        [UnityTest, Order(62)]
        public IEnumerator RemovePointsFromRequestTest()
        {

            yield return new WaitForEndOfFrame();
            WorkerInWorld workerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                workerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return workerInWorld != null && workerInWorld.World != null && workerInWorld.World.GetExistingSystem<CommandSystem>() != null;
            });
            var linkedEntityComponent = GameObject.Find("Hunter_Spawned").GetComponent<LinkedEntityComponent>();
            WorkerSystem workerSystem = linkedEntityComponent.World.GetExistingSystem<WorkerSystem>();


            EntityId hunterEntityId = linkedEntityComponent.EntityId;

            if (workerSystem.TryGetEntity(hunterEntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;
                var initialPoints = entityManager.GetComponentData<PointSchema.Point.Component>(entity);
                PointSchema.PointMetadata.Component pointMetadataComponent = entityManager.GetComponentData<PointSchema.PointMetadata.Component>(entity);
                PointSystems.PointRequestSystem pointRequestSystem = workerSystem.World.GetExistingSystem<PointSystems.PointRequestSystem>();

                PointSchema.PointRequest pointRequest = new PointSchema.PointRequest
                {
                    EntityUpdating = hunterEntityId,
                    PointUpdate = -100
                };
                PointSchema.PointResponse? response = null;
                int framesPassed = -1;
                pointRequestSystem.AddPointRequest(pointRequest, (PointSchema.PointResponse pointResponse) =>
                {
                    response = pointResponse;
                });
                yield return new WaitWhile(() => {
                    // I guess this isn't ran every frame.
                    framesPassed += 1;
                    return !response.HasValue;
                });

                var updatedPoints = entityManager.GetComponentData<PointSchema.Point.Component>(entity);
                var expectedUpdate = initialPoints.Value + (pointMetadataComponent.IdleGainRate * framesPassed) + pointRequest.PointUpdate;
                Assert.AreEqual(pointRequest.PointUpdate, response.GetValueOrDefault().TotalPoints, "Expected points not matching response");
                Assert.AreEqual(expectedUpdate, updatedPoints.Value, "Points not removed correctly");
            }
        }

        [UnityTest, Order(63)]
        public IEnumerator PointsUpdateUITest()
        {
            // Do later.
            yield return null;
        }
    }
}