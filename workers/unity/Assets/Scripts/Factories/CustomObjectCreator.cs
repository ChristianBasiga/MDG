using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using Improbable;
using MdgSchema.Player;
using MDG.Common.Components;
using MDG.Hunter.Components;
using MDG.Hunter.Commands;
using MdgSchema.Spawners;
using MdgSchema.Common;
using Unity.Transforms;
using Unity.Rendering;
using MDG.Common.Systems;

namespace MDG
{
    /// <summary>
    /// This creates corresponding game object to entity, as well as adds any extra ECS components
    /// an entity needs. Perhaps ladder can be moved to different.
    /// </summary>
    // Use zenject to install stuff here.
    public class CustomGameObjectCreator : IEntityGameObjectCreator
    {
        // Storing here prob fine actually.
        public Dictionary<EntityId, List<GameObject>> EntityToGameObjects { private set; get; }
        // Get from pool down line.
        private Dictionary<GameEntityTypes, Dictionary<bool, GameObject>> keyToPrefabs;
        private readonly IEntityGameObjectCreator _default;
        private readonly Unity.Entities.World _world;
        private readonly string _workerType;

        //Get from GameManager instanc later.
        private static readonly int width = 100;
        private static readonly int length = 100;

        // Temporary, as dynamic may also change.
        private static int startingPointsUsed = 0;

        private ComponentUpdateSystem ComponentUpdateSystem;

        //Starting points will be 10% off whatever bounds are.

        
        // These must be injected.
        private static List<Coordinates> startingPoints = new List<Coordinates>
        {
            new Coordinates(width - (width * 0.1f), 100, length * 0.6),
            new Coordinates((-width) + (width * 0.1f), 100, -length * 0.6),
        };
        public List<Vector3f> initialUnitCoordinates = new List<Vector3f>
        {
            new Vector3f( width + (width * 0.1f), 20, 0),
            new Vector3f( width + (width * 0.1f), 20, (length * 0.6f)),
            new Vector3f( width + (width * 0.1f), 20, -(length * 0.6f)),
        };

        //Look into being able to add multiple custom creators and see if can do that instead.   
        //I can still do factory plan this way.

        //Make worker type an enum to parse.
        public CustomGameObjectCreator(IEntityGameObjectCreator _default, Unity.Entities.World world, string workerType)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;
            EntityToGameObjects = new Dictionary<EntityId, List<GameObject>>();
            ComponentUpdateSystem = _world.GetExistingSystem<ComponentUpdateSystem>();
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {

            if (!entity.HasComponent<Metadata.Component>()) return;
            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";
            var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);

            //Second spawned fails somewhereg
            //Create constants page for this later on as well.
            if (metaData.EntityType.Equals("Player"))
            {
                GameMetadata.Component gameMetaData = entity.GetComponent<GameMetadata.Component>();

                //if (gameMetaData.Type == GameEntityTypes.Hunted || gameMetaData.Type == GameEntityTypes.Hunter)
                //{
                PlayerType type = entity.GetComponent<PlayerMetaData.Component>().PlayerType;

                if (hasAuthority)
                {
                    WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                    if (type == PlayerType.HUNTER)
                    {
                        Entity hunterEntity;
                        if (worker.TryGetEntity(entity.SpatialOSEntityId, out hunterEntity))
                        {
                            // Maybe snapshot type of count, that would be ideal actually.
                            _world.EntityManager.AddComponent(hunterEntity, ComponentType.ReadWrite<CommandGiver>());
                            // For this to work, I need to trigger event.
                            // and need to send event to worker 

                            // Decide best way to sort this out, prob via installer.
                            int multiplier = startingPointsUsed == 1 ? 1 : -1;


                            for (int i = 0; i < initialUnitCoordinates.Count; ++i)
                            {

                                Vector3f startingCoords = initialUnitCoordinates[i];// + startingPoints[startingPointsUsed].ToUnityVector());
                                Entity unitSpawner = _world.EntityManager.CreateEntity(typeof(Hunter.Components.UnitSpawner));
                                _world.EntityManager.SetComponentData(unitSpawner, new Hunter.Components.UnitSpawner
                                {
                                    AmountToSpawn = 1,
                                    Position = startingCoords
                                });
                                /*
                                var ent = World.Active.EntityManager.CreateEntity(typeof(RenderMesh), typeof(Unity.Transforms.LocalToWorld), typeof(Unity.Transforms.Scale),
                                typeof(Unity.Transforms.Translation), typeof(Unity.Rendering.RenderBounds), typeof(Improbable.
                                ));
                                Debug.LogError(World.Active.Name);

                                World.Active.EntityManager.SetSharedComponentData(ent, new RenderMesh { mesh = mesh, material = material });
                                World.Active.EntityManager.SetComponentData(ent, new Improbable.Position.Component { Coords = startingCoords });
                                World.Active.EntityManager.SetComponentData(ent, new Unity.Transforms.Scale { Value = 50.0f });*/
                            }

                            // So for new spawns, it needs to get ALL positions of existing entities and spawn them in my active world.
                            // for this they need to be stored in central place. Game Logic.
                        }

                    }
                    pathToEntity = $"{pathToEntity}/Authoritative";
                }
                else if (gameMetaData.Type == GameEntityTypes.Hunter)
                {
                    return;
                }

                pathToEntity = $"{pathToEntity}/{type.ToString()}";
                //GameObject.FindGameObjectWithTag("MainCamera").SetActive(false);
                GameObject created = CreateEntityObject(entity, linker, pathToEntity, null, null);
                Vector3 startingPoint = startingPoints[startingPointsUsed].ToUnityVector();
                created.transform.position = startingPoint;
                created.transform.rotation = Quaternion.identity;

                GameObject.FindGameObjectWithTag("MainCamera").SetActive(false);
            }
            else if (metaData.EntityType.Equals("Unit"))
            {
                Entity unitEntity;
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                if (worker.TryGetEntity(entity.SpatialOSEntityId, out unitEntity))
                {
                    // Custom creator essentially just acting as entity creation call back.
                    _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadWrite<Clickable>());
                    _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadWrite<UnitComponent>());
                    if (hasAuthority)
                    {
                        pathToEntity = $"{pathToEntity}/Authoritative";
                        _world.EntityManager.AddComponentData(unitEntity, new CommandListener { CommandType = CommandType.None });
                        _world.EntityManager.AddComponentData(unitEntity, new CommandMetadata { CommandType = CommandType.None });
                    }
                    else
                    {
                        _world.EntityManager.AddComponent(unitEntity, ComponentType.ReadOnly<EnemyComponent>());
                    }
                }
                pathToEntity = $"{pathToEntity}/Unit";
                GameObject gameObject = CreateEntityObject(entity, linker, pathToEntity, null, null);
                gameObject.tag = "Unit";
                gameObject.name = $"{gameObject.name} {(hasAuthority? "authoritative" : "")}";
            }
            else
            {
                _default.OnEntityCreated(entity, linker);
                return;
            }
        }

        public void OnEntityRemoved(EntityId entityId)
        {
            _default.OnEntityRemoved(entityId);
            List<GameObject> linkedGameObjects;
            if (EntityToGameObjects.TryGetValue(entityId, out linkedGameObjects))
            {
                // Destroy GameObject represnting it and remove from mappings.
                foreach (GameObject gameObject in linkedGameObjects)
                {
                    gameObject.SetActive(false);
                }
            }
           // EntitySyncSystem syncSystem = _world.GetExistingSystem<EntitySyncSystem>();
           // syncSystem.DestroyEntity(entityId);
        }

        GameObject CreateEntityObject(SpatialOSEntity entity, EntityGameObjectLinker linker, string pathToEntity, Transform parent = null, System.Type[] ecsComponents = null)
        {
            //Change to get from pool instead later on in final version of project.
            Object prefab = Resources.Load(pathToEntity);
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

            if (!EntityToGameObjects.ContainsKey(entity.SpatialOSEntityId))
            {
                EntityToGameObjects[entity.SpatialOSEntityId] = new List<GameObject>();
            }

            EntityToGameObjects[entity.SpatialOSEntityId].Add(gameObject);
            return gameObject;
        }

        public List<GameObject> GetLinkedGameObjectById(EntityId entityId)
        {
            List<GameObject> gameObjects;
            if (!EntityToGameObjects.TryGetValue(entityId, out gameObjects))
            {
                Debug.LogError("Not got");
            }
            return gameObjects;
        }
    }
}