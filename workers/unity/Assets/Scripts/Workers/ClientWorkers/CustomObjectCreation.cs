using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using Improbable;
using MdgSchema.Player;
using MdgSchema.Lobby;
using MDG.Common;
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Improbable.Worker.CInterop.Query;
using MDG.Common.Components;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;
using MdgSchema.Spawners;
using System.Linq;

namespace MDG
{
    public class CustomGameObjectCreator : IEntityGameObjectCreator
    {

        private Dictionary<EntityId, List<GameObject>> entityToGameObjects;
        private readonly IEntityGameObjectCreator _default;
        private readonly Unity.Entities.World _world;
        private readonly string _workerType;

        //SHould set parent to surface.
        public GameObject surface;
        //Get from GameManager instanc later.
        private static readonly int width = 100;
        private static readonly int length = 100;

        // Temporary, as dynamic may also change.
        private static int startingPointsUsed = 0;

        private ComponentUpdateSystem ComponentUpdateSystem;

        //Starting points will be 10% off whatever bounds are.
        private static List<Coordinates> startingPoints = new List<Coordinates>
        {
            new Coordinates(width - (width * 0.1f), 0, length * 0.6),
            new Coordinates((-width) + (width * 0.1f), 0, -length * 0.6),
        };


        private static List<Coordinates> initialUnitCoordinates = new List<Coordinates>
        {
            new Coordinates((width * 0.4f), 10, 0),
            new Coordinates((width * 0.4f), 10, (length * 0.6f)),
            new Coordinates((width * 0.4f), 10, -(length * 0.6f)),
        };

        //Look into being able to add multiple custom creators and see if can do that instead.   
        //I can still do factory plan this way.

        //Make worker type an enum to parse.
        public CustomGameObjectCreator(IEntityGameObjectCreator _default, Unity.Entities.World world, string workerType)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;
            entityToGameObjects = new Dictionary<EntityId, List<GameObject>>();
            ComponentUpdateSystem = _world.GetExistingSystem<ComponentUpdateSystem>();
            
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {

            if (!entity.HasComponent<Metadata.Component>()) return;
            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";
            var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);

            //Create constants page for this later on as well.
            if (metaData.EntityType.Equals("Player"))
            {
                PlayerType type = entity.GetComponent<PlayerMetaData.Component>().PlayerType;
               
                if (hasAuthority)
                {
                    WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                    if (type == PlayerType.HUNTER)
                    {
                        Entity hunterEntity;
                        if (worker.TryGetEntity(entity.SpatialOSEntityId, out hunterEntity))
                        {
                            _world.EntityManager.AddComponent(hunterEntity, ComponentType.ReadWrite<MouseInputComponent>());
                            _world.EntityManager.AddComponent(hunterEntity, ComponentType.ReadWrite<CommandGiver>());

                           
                            // For this to work, I need to trigger event.
                            // and need to send event to worker 
                            int multiplier = startingPointsUsed == 1 ? 1 : -1;
                            for (int i = 0; i < initialUnitCoordinates.Count; ++i)
                            {
                                Vector3 updated = (multiplier * initialUnitCoordinates[i].ToUnityVector() + startingPoints[startingPointsUsed].ToUnityVector());
                                Coordinates startingCoords = new Coordinates(updated.x, updated.y, updated.z);
                                Entity unitSpawner = _world.EntityManager.CreateEntity(typeof(UnitSpawner.Component));
                                _world.EntityManager.SetComponentData(unitSpawner, new UnitSpawner.Component
                                {
                                    AmountToSpawn = 1,
                                    Position = new Coordinates(0,0,0)
                                });
                            }
                        }

                    }
                    pathToEntity = $"{pathToEntity}/Authoritative";
                }
                else if (type == PlayerType.HUNTER)
                {
                    // Don't create gameobject for it if non authoritative as it is not a visible entity
                    return;
                }


                pathToEntity = $"{pathToEntity}/{type.ToString()}";
                GameObject created = CreateEnityObject(entity, linker, pathToEntity, null, null);
                Vector3 vector3 = startingPoints[startingPointsUsed++].ToUnityVector();
                created.transform.position = new Vector3(vector3.x, created.transform.position.y, vector3.z);
                GameObject.FindGameObjectWithTag("MainCamera").SetActive(false);
                created.tag = "MainCamera";

            }
            else if (metaData.EntityType.Equals("Unit"))
            {
                // So if have authority over Unit
                string authority = "";
                Debug.LogError(startingPointsUsed);
                // Get correct Unit GameObjectPrefab.
                // its the same entity, whether or not the current worker has authority over this entity is irrelevant.
                // if didn't before and has now then added to same fucking entity.
                Entity unitEntity;
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                if (worker.TryGetEntity(entity.SpatialOSEntityId, out unitEntity))
                {
                    _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadWrite<Clickable>());
                    _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadWrite<UnitComponent>());
                    if (hasAuthority)
                    {
                        pathToEntity = $"{pathToEntity}/Authoritative";
                        authority = "authority";
                        _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadWrite<CommandListener>());
                        //Later on actually will do adding instead of changing command meta data like attempted originally.
                        _world.EntityManager.AddComponentData(unitEntity, new CommandMetadata { CommandType = CommandType.None });
                    }
                    else
                    {
                        //If no autority then it is other play unit, meanign for POC, enemy.
                        _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadOnly<EnemyComponent>());
                    }
                }
                //then init system will apply needed components regardless
                pathToEntity = $"{pathToEntity}/Unit";
                GameObject gameObject = CreateEnityObject(entity, linker, pathToEntity,null, null);
                gameObject.tag = "Unit";
                gameObject.name = $"{gameObject.name} {authority}";
            }
            else if (metaData.EntityType.Equals("PlayerCreator"))
            {
                _default.OnEntityCreated(entity, linker);
                return;

            }
            // Scrap this.
            else if (metaData.EntityType.Equals("Lobby"))
            {
                //Then I just need to link entity to 2 different game objects. I get it now.

                //Adds server worker.
                pathToEntity = $"{pathToEntity}/{metaData.EntityType}";
                CreateEnityObject(entity, linker, pathToEntity, null, null);

                //Client worker
                Object clientLobbyPrefab = Resources.Load($"Prefabs/{UnityClientConnector.WorkerType}/Lobby");
                GameObject gameObject = Object.Instantiate(clientLobbyPrefab) as GameObject;
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
                gameObject.name = $"{gameObject.name}(SpatialOS: {entity.SpatialOSEntityId}, Worker: {UnityClientConnector.WorkerType})";
            }
            else if (metaData.EntityType.Equals("Resource"))
            {
                // Unrelated just need to note before i knock out
                // Clckable and selected by needs to be a list of selectees.
                // reasoning for this is while they have their own gameobject version.
                // it is the same entity.
                //meaning if client 1 clicks to see info, then client 2 clicks to give command
                //client 1 will no longer see info which makes no sense. That should be a per instance basis.
            }
        }

        public void OnEntityRemoved(EntityId entityId)
        {

            //Add back to pool or whatever.
            _default.OnEntityRemoved(entityId);
        }

        GameObject CreateEnityObject(SpatialOSEntity entity, EntityGameObjectLinker linker, string pathToEntity, Transform parent = null, System.Type[] ecsComponents = null)
        {
            //Change to get from pool instead later on in final version of project.
            Object prefab = Resources.Load(pathToEntity);
            //Debug.LogError(pathToEntity);
            GameObject gameObject = Object.Instantiate(prefab) as GameObject;
            if (parent)
            {
                gameObject.transform.parent = parent;
            }
            gameObject.name = $"{prefab.name}(SpatialOS: {entity.SpatialOSEntityId}, Worker: {_workerType})";
            //Seems like can inject components is that better to just add or not add or to just have diff similiar prefabs?
            if (ecsComponents != null)
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, ecsComponents);
            }
            else
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
            }

            if (!entityToGameObjects.ContainsKey(entity.SpatialOSEntityId))
            {
                entityToGameObjects[entity.SpatialOSEntityId] = new List<GameObject>();
            }

            entityToGameObjects[entity.SpatialOSEntityId].Add(gameObject);


            return gameObject;
        }

        public List<GameObject> GetLinkedGameObjectById(EntityId entityId)
        {
            // Throw exception.
            List<GameObject> gameObjects;
            if (!entityToGameObjects.TryGetValue(entityId, out gameObjects))
            {
                Debug.LogError("Not got");
            }
            return gameObjects;
        }
    }
}