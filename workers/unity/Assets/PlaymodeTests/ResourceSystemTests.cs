using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using MDG;
using MDG.Common.Components;
using MDG.Invader.Components;
using MdgSchema.Common;
using MdgSchema.Units;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CommonSystems = MDG.Common.Systems;
using SpawnSystems = MDG.Common.Systems.Spawn;
using SpawnSchema = MdgSchema.Common.Spawn;
using ResourceSchema = MdgSchema.Game.Resource;
using PointSchema = MdgSchema.Common.Point;
using PointSystems = MDG.Common.Systems.Point;
using DefenderComponents = MDG.Defender.Components;
using MDG.Common.MonoBehaviours;

using Improbable;
using MDG.Invader.Systems;

namespace PlaymodeTests
{
    public class ResourceSystemTests : IPrebuildSetup
    {
        CommandSystem commandSystem;
        GameObject clientWorker;
        GameObject serverWorker;
        GameObject pointUI;
        public void Setup()
        {
            /*clientWorker = new GameObject("ClientWorker");
            var clientConnector = clientWorker.AddComponent<UnityClientConnector>();
            serverWorker = new GameObject("GameLogicWorker");
            serverWorker.AddComponent<UnityGameLogicConnector>();*/
        }

        // Might scrap nav stuff since it blocks me but for now thi si sfine,
        [UnityTest, Order(69)]
        public IEnumerator SceneSetUp()
        {
            SceneManager.LoadScene("DevelopmentScene");
            GameObject uiManager = null;

            yield return new WaitUntil(() =>
            {
                uiManager = GameObject.Find("UIManager");
                return uiManager != null;

            });
            yield return new WaitForSeconds(2.0f);
            uiManager.GetComponent<MainOverlayHUD>().SelectRole("Hunter");
            yield return new WaitUntil(() =>
            {
                return GameObject.Find("Hunter_Spawned") != null && GameObject.FindGameObjectWithTag("Unit") != null;
            });
        }

        [UnityTest, Order(70)]
        public IEnumerator ResourceSpawnTest()
        {
            LinkedEntityComponent linkedInvader = GameObject.Find("Hunter_Spawned").GetComponent<LinkedEntityComponent>();


            // Spawn Invader to spawn units.
            SpawnSystems.SpawnRequestSystem spawnReqSystem = linkedInvader.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
            // Spawn Resource.
            EntityId resourceId = new EntityId(-1);
            spawnReqSystem.RequestSpawn(new SpawnSchema.SpawnRequest
            {
                TypeId = 1,
                TypeToSpawn = GameEntityTypes.Resource
            }, (EntityId id) =>
            {
                resourceId = id;
            });
            yield return new WaitUntil(() =>
            {
                return resourceId.IsValid();
            });

            // Make sure it has no occupants.
            WorkerSystem workerSystem = linkedInvader.Worker;
            GameObject resource = GameObject.FindGameObjectWithTag("Resource");
            Assert.IsTrue(resource != null);

            LinkedEntityComponent linkedEntityResource = resource.GetComponent<LinkedEntityComponent>();
            Assert.IsTrue(linkedEntityResource != null);
            EntityId entityId = linkedEntityResource.EntityId;
            if (workerSystem.TryGetEntity(entityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;
                Assert.True(entityManager.HasComponent<ResourceSchema.ResourceMetadata.Component>(entity)
                    && entityManager.HasComponent<PointSchema.PointMetadata.Component>(entity)
                    && entityManager.GetComponentData<Metadata.Component>(entity).EntityType.Equals("Resource")
                    && entityManager.HasComponent<ResourceSchema.Resource.Component>(entity),
                    "Resource was not spawned with correct components");
            }
        }


        [UnityTest, Order(71)]
        public IEnumerator ResourceOccupyTest()
        {
            GameObject unitObject = GameObject.FindGameObjectWithTag("Unit");
            LinkedEntityComponent linkedUnit = unitObject.GetComponent<LinkedEntityComponent>();
            GameObject resourceObject = GameObject.FindGameObjectWithTag("Resource");
            LinkedEntityComponent linkedResource = resourceObject.GetComponent<LinkedEntityComponent>();
            ResourceRequestSystem resourceRequestSystem = linkedResource.World.GetExistingSystem<ResourceRequestSystem>();

            ResourceRequestSystem.ResourceRequestReponse? occupyResponse = null;
            resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
            {
                OccupantId = linkedUnit.EntityId,
                ResourceId = linkedResource.EntityId,
                ResourceRequestType = ResourceSchema.ResourceRequestType.OCCUPY,
                callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                {
                    occupyResponse = response;
                }
            });
            yield return new WaitUntil(() => { return occupyResponse.HasValue; });

            Assert.AreEqual(linkedResource.EntityId, occupyResponse.Value.EffectedResource, "Response of effected resource not matching resource" +
                "wanted to occupy");
            Assert.IsTrue(occupyResponse.Value.Success, "Failed to occupy an empty occupant resource");

            WorkerSystem workerSystem = linkedResource.Worker;

            if (workerSystem.TryGetEntity(linkedResource.EntityId, out Entity entity))
            {
                EntityManager entityManager = workerSystem.EntityManager;

                ResourceSchema.Resource.Component occupiedComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

                Assert.Contains(linkedUnit.EntityId, occupiedComponent.Occupants, "Occupants of resource does not contain unit that occupied it");

                // Prob it's own tset case, this is mor elike single suite.
                var resourceComponent = entityManager.GetComponentData<ResourceSchema.ResourceMetadata.Component>(entity);
                Assert.LessOrEqual(occupiedComponent.Occupants.Count, resourceComponent.MaximumOccupancy, "Added occupant to already filled resource");
            }
        }

        [UnityTest, Order(72)]
        public IEnumerator ResourceCollectWhileOccupiedTest()
        {

            // Move this boilerplate to own Ienumerator and yield on it.
            // test that actually being possible.
            GameObject unitObject = GameObject.FindGameObjectWithTag("Unit");
            LinkedEntityComponent linkedUnit = unitObject.GetComponent<LinkedEntityComponent>();
            GameObject resourceObject = GameObject.FindGameObjectWithTag("Resource");
            LinkedEntityComponent linkedResource = resourceObject.GetComponent<LinkedEntityComponent>();
            ResourceRequestSystem resourceRequestSystem = linkedResource.World.GetExistingSystem<ResourceRequestSystem>();


            WorkerSystem workerSystem = linkedResource.Worker;
            EntityManager entityManager = workerSystem.EntityManager;

            workerSystem.TryGetEntity(linkedResource.EntityId, out Entity entity);
            var initialOccupiedComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);
            Assert.Contains(linkedUnit.EntityId, initialOccupiedComponent.Occupants, "Resource collecting is not occupied by requesting unit");
            ResourceRequestSystem.ResourceRequestReponse? collectResponse = null;

            resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
            {
                OccupantId = linkedUnit.EntityId,
                ResourceId = linkedResource.EntityId,
                ResourceRequestType = ResourceSchema.ResourceRequestType.COLLECT,
                callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                {
                    collectResponse = response;
                }
            });
            yield return new WaitUntil(() => { return collectResponse.HasValue; });

            ResourceSchema.Resource.Component occupiedComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

            Assert.IsTrue(collectResponse.Value.Success);
            Assert.Contains(linkedUnit.EntityId, occupiedComponent.Occupants, "Resource collecting is not occupied by requesting unit");
            Assert.AreEqual(linkedResource.EntityId, collectResponse.Value.EffectedResource,
                "Response of effected resource not matching resource wanted to collect");
            Assert.Less(occupiedComponent.Health, initialOccupiedComponent.Health);
            // Here I need collect rate. It should just be less, collect rate is more an invader unit test.
            Assert.AreEqual(occupiedComponent.Health, initialOccupiedComponent.Health - 1);
        }   

        [UnityTest, Order(73)]
        public IEnumerator ResourceReleaseTest()
        {
            // Move this boilerplate to own Ienumerator and yield on it.
            // test that actually being possible.
            GameObject unitObject = GameObject.FindGameObjectWithTag("Unit");
            LinkedEntityComponent linkedUnit = unitObject.GetComponent<LinkedEntityComponent>();
            GameObject resourceObject = GameObject.FindGameObjectWithTag("Resource");
            LinkedEntityComponent linkedResource = resourceObject.GetComponent<LinkedEntityComponent>();
            ResourceRequestSystem resourceRequestSystem = linkedResource.World.GetExistingSystem<ResourceRequestSystem>();


            WorkerSystem workerSystem = linkedResource.Worker;
            EntityManager entityManager = workerSystem.EntityManager;

            workerSystem.TryGetEntity(linkedResource.EntityId, out Entity entity);

            ResourceRequestSystem.ResourceRequestReponse? releaseResponse = null;

            resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
            {
                OccupantId = linkedUnit.EntityId,
                ResourceId = linkedResource.EntityId,
                ResourceRequestType = ResourceSchema.ResourceRequestType.RELEASE,
                callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                {
                    releaseResponse = response;
                }
            });
            yield return new WaitUntil(() => { return releaseResponse.HasValue; });
            var updatedResourceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);
            Assert.False(updatedResourceComponent.Occupants.Contains(linkedUnit.EntityId), "Failed to release unit from resource");
            Assert.IsTrue(releaseResponse.Value.Success);

        }

        [UnityTest, Order(74)]
        public IEnumerator OccupyFullTest()
        {
            GameObject unitObject = GameObject.FindGameObjectWithTag("Unit");
            LinkedEntityComponent linkedUnit = unitObject.GetComponent<LinkedEntityComponent>();
            GameObject resourceObject = GameObject.FindGameObjectWithTag("Resource");
            LinkedEntityComponent linkedResource = resourceObject.GetComponent<LinkedEntityComponent>();
            ResourceRequestSystem resourceRequestSystem = linkedResource.World.GetExistingSystem<ResourceRequestSystem>();


            WorkerSystem workerSystem = linkedResource.Worker;
            EntityManager entityManager = workerSystem.EntityManager;

            workerSystem.TryGetEntity(linkedResource.EntityId, out Entity entity);
            var resourceMetadataComponent = entityManager.GetComponentData<ResourceSchema.ResourceMetadata.Component>(entity);
            ResourceRequestSystem.ResourceRequestReponse? occupyResponse = null;

            var resourceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);
            for (int i = 1; i < resourceMetadataComponent.MaximumOccupancy + 2; ++i)
            {

                if (i != linkedUnit.EntityId.Id)
                {
                    occupyResponse = null;
                    resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                    {
                        OccupantId = new EntityId(i),
                        ResourceId = linkedResource.EntityId,
                        ResourceRequestType = ResourceSchema.ResourceRequestType.OCCUPY,
                        callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                        {
                            occupyResponse = response;
                        }
                    });
                    yield return new WaitUntil(() => { return occupyResponse.HasValue; });
                }
            }

            var updatedResourceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

            Assert.AreEqual(updatedResourceComponent.Occupants.Count, resourceMetadataComponent.MaximumOccupancy, "Failed to hit max occupancy of resource");

            occupyResponse = null;
            LogAssert.Expect(LogType.Error, $"Resource {linkedResource.EntityId} is fully occupied");
            resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
            {
                OccupantId = linkedUnit.EntityId,
                ResourceId = linkedResource.EntityId,
                ResourceRequestType = ResourceSchema.ResourceRequestType.OCCUPY,
                callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                {
                    occupyResponse = response;
                }
            });
            yield return new WaitUntil(() => { return occupyResponse.HasValue; });

            updatedResourceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);

            Assert.IsFalse(updatedResourceComponent.Occupants.Contains(linkedUnit.EntityId), "Added entity to already filled resource");
            Assert.IsFalse(occupyResponse.Value.Success, "Marked failure as success");
            Assert.AreEqual(updatedResourceComponent.Occupants.Count, resourceMetadataComponent.MaximumOccupancy, "Went over max occupany");

        }

        [UnityTest, Order(75)]
        public IEnumerator CollectDepleteTest()
        {
            GameObject unitObject = GameObject.FindGameObjectWithTag("Unit");
            LinkedEntityComponent linkedUnit = unitObject.GetComponent<LinkedEntityComponent>();
            GameObject resourceObject = GameObject.FindGameObjectWithTag("Resource");
            LinkedEntityComponent linkedResource = resourceObject.GetComponent<LinkedEntityComponent>();
            ResourceRequestSystem resourceRequestSystem = linkedResource.World.GetExistingSystem<ResourceRequestSystem>();


            WorkerSystem workerSystem = linkedResource.Worker;
            EntityManager entityManager = workerSystem.EntityManager;

            workerSystem.TryGetEntity(linkedResource.EntityId, out Entity entity);
            var resourceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);
            int currHealth = resourceComponent.Health;
            while (currHealth > 0)
            {
                currHealth -= 1;
                ResourceRequestSystem.ResourceRequestReponse? collectResponse = null;

                // in resource request system, down line could batch these prior to 
                // avoid overkill of round trips. Though prob not huge hindrance tbh.
                resourceRequestSystem.SendRequest(new ResourceRequestSystem.ResourceRequestHeader
                {
                    OccupantId = linkedUnit.EntityId,
                    ResourceId = linkedResource.EntityId,
                    ResourceRequestType = ResourceSchema.ResourceRequestType.COLLECT,
                    callback = (ResourceRequestSystem.ResourceRequestReponse response) =>
                    {
                        collectResponse = response;
                    }
                });
                yield return new WaitUntil(() => { return collectResponse.HasValue; });
                if (currHealth == 0)
                {
                    Assert.AreEqual(currHealth, collectResponse.Value.CollectResponse.Value.TimesUntilDepleted, "Did not deplete health down");
                    Assert.AreEqual(linkedUnit.EntityId, collectResponse.Value.CollectResponse.Value.DepleterId.Value, "Incorrent entity id marked as depleter");
                }
            }
            var resourceMetadataComponent = entityManager.GetComponentData<ResourceSchema.ResourceMetadata.Component>(entity);

            var updatedResoruceComponent = entityManager.GetComponentData<ResourceSchema.Resource.Component>(entity);
            Assert.AreEqual(0, updatedResoruceComponent.Health, "Failed to update health correctly");
            Assert.IsEmpty(updatedResoruceComponent.Occupants, "Failed to clear occupants");
            yield return new WaitForEndOfFrame();

            if (resourceMetadataComponent.WillRespawn)
            {
                yield return new WaitForSeconds(resourceMetadataComponent.RespawnTime);
                // 3 frames for, sending delete request, processing delete on server, then running callback to make inactive
                // in main thread.
                for (int i = 0; i < 3; ++i)
                {
                    yield return new WaitForEndOfFrame();
                }
                Assert.IsFalse(resourceObject.activeInHierarchy, "Did not despawn previous resource");
                GameObject respawnedResource = GameObject.FindGameObjectWithTag("Resource");
                Assert.True(respawnedResource != null, "Failed to respawn resource");
                Assert.AreNotEqual(linkedResource.EntityId, respawnedResource.GetComponent<LinkedEntityComponent>().EntityId, "Failed to spawn new resource");
            }
            else
            {
                Assert.False(workerSystem.TryGetEntity(linkedResource.EntityId, out _), "failed to delete depleted resource");
            }
        }
    }
}