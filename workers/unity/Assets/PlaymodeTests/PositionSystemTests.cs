using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MDG.Common.Systems;
using MDG.Common.Systems.Spawn;
using MDG;
using Improbable.Gdk.Core;
using MDG.Common.Systems.Position;
using SpawnSchema = MdgSchema.Common.Spawn;
using PositionSchema = MdgSchema.Common.Position;
using MDG.Common.Datastructures;
using Improbable;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using System.Linq;
namespace PlaymodeTests
{
    // These should be suites.
    public class PositionSystemTests : IPrebuildSetup
    {
        WorkerSystem workerSystem;
        CommandSystem commandSystem;
        SpawnRequestSystem spawnReqSystem;
        PositionSystem positionSystem;
        GameObject clientWorker;
        GameObject serverWorker;
        public void Setup()
        {
            clientWorker = new GameObject("ClientWorker");
            clientWorker.AddComponent<UnityClientConnector>();

            serverWorker = new GameObject("GameLogicWorker");
            serverWorker.AddComponent<UnityGameLogicConnector>();
        }

        // Spawns entity and makes sure spawned entity is added to quad tree.
        [UnityTest, Order(80)]
        public IEnumerator AddToSpatialPartitionTest()
        {

            WorkerInWorld clientWorkerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                clientWorkerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return clientWorkerInWorld != null && clientWorkerInWorld.World != null;
            });
            WorkerInWorld serverWorkerInWorld = null;

            yield return new WaitUntil(() =>
            {
                serverWorker = GameObject.Find("GameLogicWorker");
                serverWorkerInWorld = serverWorker.GetComponent<UnityGameLogicConnector>().Worker;
                return serverWorkerInWorld != null && serverWorkerInWorld.World != null;
            });
            spawnReqSystem = clientWorkerInWorld.World.GetExistingSystem<SpawnRequestSystem>();
            positionSystem = serverWorkerInWorld.World.GetExistingSystem<PositionSystem>();
            SpawnSchema.SpawnRequest payload = new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Position = new Improbable.Vector3f(1, 1, 1)
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

            QuadNode? queryByIdPotential = positionSystem.querySpatialPartition(unitEntityId);
            Assert.IsTrue(queryByIdPotential.HasValue, $"Entity id {unitEntityId} not added to spatial partition structure");
            QuadNode queryById = queryByIdPotential.Value;
            Assert.AreEqual(queryById.position, payload.Position, $"Entity id {unitEntityId} not placed in correct position from id query.");

            List<QuadNode> queryByPosition = positionSystem.querySpatialPartition(payload.Position);
            Assert.IsNotEmpty(queryByPosition, $"Entity id {unitEntityId} not placed in correct position from position query");

            Assert.AreEqual(queryByPosition[0], queryById, "Query via id returned different result from query via position");

            Vector3f dimensions = queryById.dimensions;
            Vector3f centerOfRegion = queryById.center;

            Assert.IsTrue((payload.Position.X <= centerOfRegion.X + dimensions.X / 2)
                && (payload.Position.X >= centerOfRegion.X - dimensions.X / 2)
                && (payload.Position.Z <= centerOfRegion.Z + dimensions.Z / 2)
                && (payload.Position.Z >= centerOfRegion.Z - dimensions.Z / 2), $"Entity ${unitEntityId} not placed in correct region");
        }

        [UnityTest, Order(81)]
        public IEnumerator SubdivideCorrectlyTest()
        {
            WorkerInWorld clientWorkerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                clientWorkerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return clientWorkerInWorld != null && clientWorkerInWorld.World != null;
            });
            WorkerInWorld serverWorkerInWorld = null;

            yield return new WaitUntil(() =>
            {
                serverWorker = GameObject.Find("GameLogicWorker");
                serverWorkerInWorld = serverWorker.GetComponent<UnityGameLogicConnector>().Worker;
                return serverWorkerInWorld != null && serverWorkerInWorld.World != null;
            });
            spawnReqSystem = clientWorkerInWorld.World.GetExistingSystem<SpawnRequestSystem>();
            positionSystem = serverWorkerInWorld.World.GetExistingSystem<PositionSystem>();

            List<SpawnSchema.SpawnRequest> spawnedEntityToPayload = new List<SpawnSchema.SpawnRequest>();
            List<EntityId> entityIds = new List<EntityId>();
            // Spawns 4 entities halved randomly placed in what should be 2 seperate regions.
            for (int i = 0; i < positionSystem.RegionCapacity * 2; ++i)
            {
                float randomX = Random.Range(0, positionSystem.RootDimensions.X / 2);
                if (i >= 2)
                {
                    randomX = Random.Range(positionSystem.RootDimensions.X / 2, positionSystem.RootDimensions.X);
                }
                SpawnSchema.SpawnRequest payload = new SpawnSchema.SpawnRequest
                {
                    TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                    Position = new Improbable.Vector3f(randomX, 0, 1)
                };
                EntityId unitEntityId = new EntityId(-1);
                spawnReqSystem.RequestSpawn(payload,
                    (EntityId id) =>
                    {
                        unitEntityId = id;
                    }
                );
                yield return new WaitUntil(() => { return unitEntityId.IsValid(); });
                spawnedEntityToPayload.Add(payload);
                entityIds.Add(unitEntityId);
                yield return new WaitForEndOfFrame();
            }

            // Assert on same regions per half.
            List<QuadNode> leftHalfQuery = positionSystem.querySpatialPartition(spawnedEntityToPayload[0].Position);
            Assert.True(leftHalfQuery.Any((QuadNode qn) =>
            {
                return qn.entityId.Equals(entityIds[0]);
            }), "Left Half region placed entities not grouped together. Not inserting correctly");

            List<QuadNode> rightHalfQuery = positionSystem.querySpatialPartition(spawnedEntityToPayload[2].Position);

            Assert.True(rightHalfQuery.Any((QuadNode qn) =>
            {
                return qn.entityId.Equals(entityIds[3]);
            }), "Right Half region placed entities not grouped together. Not inserting correctly");

            Assert.False(leftHalfQuery.Any((QuadNode qn) =>
            {
                return qn.entityId.Equals(entityIds[2]);
            }), "Left half placed and right half placed regions are grouped together. Not subdivided correctly");
            // Assert on diff regions between other halfs.
        }

        [UnityTest, Order(81)]
        public IEnumerator UpdatePositionFromLinearVelocityCorrectly()
        {
            WorkerInWorld clientWorkerInWorld = null;
            yield return new WaitUntil(() =>
            {
                clientWorker = GameObject.Find("ClientWorker");
                clientWorkerInWorld = clientWorker.GetComponent<UnityClientConnector>().Worker;
                return clientWorkerInWorld != null && clientWorkerInWorld.World != null;
            });
            WorkerInWorld serverWorkerInWorld = null;

            yield return new WaitUntil(() =>
            {
                serverWorker = GameObject.Find("GameLogicWorker");
                serverWorkerInWorld = serverWorker.GetComponent<UnityGameLogicConnector>().Worker;
                return serverWorkerInWorld != null && serverWorkerInWorld.World != null;
            });

            spawnReqSystem = clientWorkerInWorld.World.GetExistingSystem<SpawnRequestSystem>();
            positionSystem = serverWorkerInWorld.World.GetExistingSystem<PositionSystem>();
            workerSystem = clientWorkerInWorld.World.GetExistingSystem<WorkerSystem>();

            SpawnSchema.SpawnRequest payload = new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Position = new Improbable.Vector3f(1, 1, 1)
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

            LinkedEntityComponent linkedEntityComponent = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>();
            Vector3f linearVelocityApplied = new Vector3f(10, 0, 5);
            if (workerSystem.TryGetEntity(linkedEntityComponent.EntityId, out Unity.Entities.Entity entity))
            {
                Vector3f initialPos = workerSystem.World.EntityManager.GetComponentData<EntityTransform.Component>(entity).Position;
                clientWorkerInWorld.World.EntityManager.SetComponentData(entity, new PositionSchema.LinearVelocity.Component
                {
                    Velocity = linearVelocityApplied
                });
                // frames for component add, component sync with server, run job, resync with client
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                float thisFrameDelaTime = Time.deltaTime;
                yield return new WaitForEndOfFrame();
                Vector3f updatedPosition = workerSystem.World.EntityManager.GetComponentData<EntityTransform.Component>(entity).Position;
                Assert.AreNotEqual(initialPos, updatedPosition, "Linear Velocity not applied to position");
                Assert.AreEqual(initialPos + (linearVelocityApplied * thisFrameDelaTime), updatedPosition, "Linear Velocity not applied correctly");
            }
           
        }
    }
}

