using System.Collections;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using MDG;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CollisionSchema = MdgSchema.Common.Collision;
using CollisionSystems = MDG.Common.Systems.Collision;
using SpawnSchema = MdgSchema.Common.Spawn;
using PositionSchema = MdgSchema.Common.Position;
using SpawnSystems = MDG.Common.Systems.Spawn;
using PositionSystems = MDG.Common.Systems.Position;
using MDG.Common.Datastructures;

namespace PlaymodeTests
{
    public class CollisionSystemTests : IPrebuildSetup
    {

        GameObject clientWorker;
        GameObject serverWorker;

        public void Setup()
        {
            clientWorker = new GameObject("ClientWorker");
            clientWorker.AddComponent<UnityClientConnector>();

            serverWorker = new GameObject("GameLogicWorker");
            serverWorker.AddComponent<UnityGameLogicConnector>();
        }

        // This is enough.
        [UnityTest, Order(901)]
        public IEnumerator CollisionDetectionPreventsOverlap()
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

            SpawnSystems.SpawnRequestSystem spawnRequestSystem = clientWorkerInWorld.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();

            // I know their colliders are fairly, big so I want to make sure they far aprt.

            EntityId firstSpawned = new EntityId(-1);
            Vector3f firstPosition = new Vector3f(20, 0, 0);
            spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Count = 1,
                Position = firstPosition
            }, (EntityId spawned) =>
            {
                firstSpawned = spawned;
            });
            yield return new WaitUntil(() =>
            {
                return firstSpawned.IsValid();
            });


            WorkerSystem workerSystem = spawnRequestSystem.World.GetExistingSystem<WorkerSystem>();

            workerSystem.TryGetEntity(firstSpawned, out Unity.Entities.Entity firstEntity);

            CollisionSchema.BoxCollider.Component boxCollider = workerSystem.EntityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(firstEntity);

            EntityId secondSpawned = new EntityId(-1);

            // So it should be within the collider of first, barely at edge.
            Vector3f secondPosition = firstPosition + new Vector3f(boxCollider.Dimensions.X * 2, 0, 0);

            spawnRequestSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                Count = 1,
                Position = secondPosition
            }, (EntityId spawned) =>
            {
                secondSpawned = spawned;
            });
            yield return new WaitUntil(() =>
            {
                return secondSpawned.IsValid();
            });

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();


            PositionSystems.PositionSystem positionSystem = serverWorkerInWorld.World.GetExistingSystem<PositionSystems.PositionSystem>();
            List<QuadNode> region = positionSystem.querySpatialPartition(firstPosition);
            Assert.AreEqual(2, region.Count, "Entities not added to same region");
            Assert.IsTrue(region.FindIndex((QuadNode qn) =>
            {
                return qn.entityId.Equals(secondSpawned);
            }) != -1, "Entities not in same region");
            workerSystem.TryGetEntity(secondSpawned, out Unity.Entities.Entity secondEntity);

            workerSystem.EntityManager.SetComponentData(secondEntity, new PositionSchema.LinearVelocity.Component
            {
                Velocity = firstPosition - secondPosition
            });
            CollisionSchema.Collision.Component firstCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(firstEntity);
            CollisionSchema.Collision.Component secondCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(secondEntity);
            yield return new WaitUntil(() =>
            {
                secondCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(secondEntity);
                firstCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(firstEntity);
                return secondCollision.Collisions.ContainsKey(firstSpawned);
            });
            

            Assert.IsTrue(!firstCollision.Collisions.ContainsKey(firstSpawned), "Collides with self");
            Assert.IsTrue(secondCollision.Collisions.ContainsKey(firstSpawned), "Second entity does not register first entity colliding with it");
            yield return new WaitUntil(() =>
            {
                secondCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(secondEntity);
                firstCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(firstEntity);
                return !secondCollision.Collisions.ContainsKey(firstSpawned);
            });
            firstCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(firstEntity);
            secondCollision = workerSystem.EntityManager.GetComponentData<CollisionSchema.Collision.Component>(secondEntity);
            Assert.IsFalse(firstCollision.Collisions.ContainsKey(secondSpawned), "First entity still collides with second");
            Assert.IsFalse(secondCollision.Collisions.ContainsKey(firstSpawned), "Second entity still collides with first");
        }
        [UnityTest, Order(902)]
        public IEnumerator DoReroute()
        {

            yield return null;
        }


    }
}
