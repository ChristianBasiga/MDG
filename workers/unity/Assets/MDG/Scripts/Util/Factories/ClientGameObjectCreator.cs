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
using MDG.Invader.Components;
using MDG.Invader.Commands;
using UnitSchema = MdgSchema.Units;
using WeaponSchema = MdgSchema.Common.Weapon;
using StructureSchema = MdgSchema.Common.Structure;
using MdgSchema.Common;
using Unity.Transforms;
using MDG.Common.Systems;
using Templates = MDG.Templates;
using SpawnSystems = MDG.Common.Systems.Spawn;
using InvaderSystems =  MDG.Invader.Systems;
using MdgSchema.Units;
using MDG.Templates;
using MDG.DTO;
using GameScriptableObjects = MDG.ScriptableObjects.Game;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MDG.Common.Systems.Spawn;
using MDG.ScriptableObjects.Game;
using MdgSchema.Game.Territory;

namespace MDG
{

    /// <summary>
    /// This creates corresponding game object to entity, as well as adds any extra ECS components
    /// an entity needs. Perhaps ladder can be moved to different.
    /// </summary>
    // Use zenject to install stuff here.
    public class ClientGameObjectCreator : IEntityGameObjectCreator
    {
        // For others to know when thish happens.
        public event System.Action<GameObject> OnGameObjectSpawned;
        public event System.Action<SpatialOSEntity> OnEntityAdded;
        public event System.Action<EntityId> OnEntityDeleted;
        // Storing here prob fine actually.
        public Dictionary<EntityId,GameObject> EntityToGameObjects { private set; get; }
        private readonly IEntityGameObjectCreator _default;
        private readonly Unity.Entities.World _world;
        private readonly string _workerType;
        private ComponentUpdateSystem ComponentUpdateSystem;
        GameConfig gameConfig;

        // Link to player at this client.
        LinkedEntityComponent playerLink; 
        public LinkedEntityComponent PlayerLink
        {
            get
            {
                if (playerLink == null)
                {
                    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                    if (playerObject != null){
                        playerLink = playerObject.GetComponent<LinkedEntityComponent>();
                    }
                }
                return playerLink;
            }
        }

        public List<LinkedEntityComponent> otherPlayerLinks { private set; get; }

        public ClientGameObjectCreator(IEntityGameObjectCreator _default, Unity.Entities.World world, string workerType, GameConfig gameConfig)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;
            this.gameConfig = gameConfig;
            EntityToGameObjects = new Dictionary<EntityId, GameObject>();
            otherPlayerLinks = new List<LinkedEntityComponent>();
            ComponentUpdateSystem = _world.GetExistingSystem<ComponentUpdateSystem>();
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.HasComponent<Metadata.Component>()) return;
            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}"; 

            if (metaData.EntityType.Equals("Player"))
            {
                bool hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);
                string pathToPlayer = hasAuthority ? $"{pathToEntity}/Authoritative" : pathToEntity;

                LinkedEntityComponent playerLink = PlayerLink;
                if (playerLink != null)
                {
                    Debug.Log("Player link not null " + playerLink.name);
                    // Needs to be based on connection params incase object not spawned yet upon 
                    // connection unless I can yield selecting role untill all spawned. Has to be connection param based to do it right.
                    playerLink.Worker.TryGetEntity(playerLink.EntityId, out Entity playerEntity);
                    GameMetadata.Component gameMetadata = playerLink.World.EntityManager.GetComponentData<GameMetadata.Component>(playerEntity);
                    if (gameMetadata.Type == GameEntityTypes.Hunted)
                    {
                        pathToPlayer = $"{pathToEntity}/Defender";
                    }
                    else if (gameMetadata.Type == GameEntityTypes.Hunter)
                    {
                        pathToEntity = $"{pathToEntity}/Invader";
                    }
                }


                GameMetadata.Component gameMetaData = entity.GetComponent<GameMetadata.Component>();
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity ecsEntity);
                GameEntityTypes type = gameMetaData.Type;

                if (type == GameEntityTypes.Hunter)
                {
                    PlayerArchtypes.AddInvaderArchtype(worker.EntityManager, ecsEntity, hasAuthority);
                }
                else
                {
                    PlayerArchtypes.AddDefenderArchtype(worker.EntityManager, ecsEntity, hasAuthority);
                }

                pathToPlayer = $"{pathToPlayer}/{type.ToString()}";
                Debug.Log("Path to Player " + pathToPlayer);
                GameObject g = CreateEntityObject(entity, linker, pathToPlayer);
                if (!hasAuthority)
                {
                    otherPlayerLinks.Add(g.GetComponent<LinkedEntityComponent>());
                }
            }
            else if (metaData.EntityType.Equals("Unit"))
            {
                UnitSchema.Unit.Component unitComponent = entity.GetComponent<UnitSchema.Unit.Component>();
                bool hasAuthority = PlayerLink != null && unitComponent.OwnerId.Equals(PlayerLink.EntityId);
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                if (worker.TryGetEntity(entity.SpatialOSEntityId, out Entity unitEntity))
                {
                    Templates.UnitArchtypes.AddUnitArchtype(worker.EntityManager, unitEntity, hasAuthority, unitComponent.Type);
                }
                pathToEntity = hasAuthority ? $"{pathToEntity}/Authoritative" : pathToEntity;
                pathToEntity = $"{pathToEntity}/Unit";
                CreateEntityObject(entity, linker, pathToEntity, null, null);

            }
            else if (metaData.EntityType.Equals("Resource"))
            {
             //   pathToEntity = $"{pathToEntity}/Resource";
             //   CreateEntityObject(entity, linker, pathToEntity, null, null);
            }
            else if (metaData.EntityType.Equals("Weapon"))
            {
                WeaponSchema.Weapon.Component weaponComponent = entity.GetComponent<WeaponSchema.Weapon.Component>();
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity weaponEntity);
                bool hasAuthority = PlayerLink != null && PlayerLink.EntityId.Equals(weaponComponent.WielderId);
                WeaponArchtypes.AddWeaponArchtype(_world.EntityManager, weaponEntity, hasAuthority);
                if (hasAuthority)
                {
                    pathToEntity = $"{pathToEntity}/Authoritative";
                }
                pathToEntity = $"{pathToEntity}/Weapons/{weaponComponent.WeaponId}";

                CreateEntityObject(entity, linker, pathToEntity, null, null);
            }
            else if (metaData.EntityType.Equals("Territory"))
            {
                entity.TryGetComponent(out Territory.Component territoryComponent);
                pathToEntity = $"{pathToEntity}/{territoryComponent.TerritoryId}";
                CreateEntityObject(entity, linker, pathToEntity);
            }
            else if (metaData.EntityType.Equals("Structure"))
            {
                // add owner to structure id.
                StructureSchema.StructureMetadata.Component structureMetaData = entity.GetComponent<StructureSchema.StructureMetadata.Component>();

                bool hasAuthority = PlayerLink != null && structureMetaData.OwnerId.Equals(PlayerLink.EntityId);
                string structurePath = hasAuthority ? $"{pathToEntity}/Authoritative/Structures" : $"{pathToEntity}/Structures";
                switch (structureMetaData.StructureType)
                {
                    case StructureSchema.StructureType.Trap:
                        StructureSchema.Trap.Component trap = entity.GetComponent<StructureSchema.Trap.Component>();
                        structurePath = $"{structurePath}/Traps/{trap.PrefabName}";
                        break;
                    case StructureSchema.StructureType.Spawning:
                        structurePath = $"{structurePath}/InvaderStructures/UnitSpawnStructure";
                        break;
                    case StructureSchema.StructureType.Claiming:
                        structurePath = $"{structurePath}/InvaderStructures/ClaimingStructure";
                        break;
                }
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity structureEntity);
                Templates.StructureArchtypes.AddStructureArchtype(_world.EntityManager, structureEntity, hasAuthority);
                CreateEntityObject(entity, linker, structurePath);
            }
            else
            {
                _default.OnEntityCreated(entity, linker);
            }
            OnEntityAdded?.Invoke(entity);
        }

        public void OnEntityRemoved(EntityId entityId)
        {
           // Debug.Log("Deleting entityID " + entityId);
            _default.OnEntityRemoved(entityId);
            GameObject linkedGameObject;

            if (EntityToGameObjects.TryGetValue(entityId, out linkedGameObject))
            {
                // Perhaps custom death and / or death behaviour that plays specific animation.
                // Or both. Regardless doesn't just make it dissapear.
                if (Application.isPlaying && linkedGameObject.GetComponent<HealthSynchronizer>() != null)
                {
                    return;
                }
                if (linkedGameObject.CompareTag("Player"))
                {
                    // Clean up all entities it owns.
                    // Remove active systems.
                }
                // Add check for reusable.
                else
                {
                    UnityObjectDestroyer.Destroy(linkedGameObject);
                }
            }
            EntityToGameObjects.Remove(entityId);
            OnEntityDeleted?.Invoke(entityId);
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
            if (ecsComponents != null)
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, ecsComponents);
            }
            else
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
            }
            EntityToGameObjects[entity.SpatialOSEntityId] = gameObject;
            OnGameObjectSpawned?.Invoke(gameObject);
            return gameObject;
        }

        public GameObject GetLinkedGameObjectById(EntityId entityId)
        {
            GameObject linkedObject = null;
            if (!EntityToGameObjects.TryGetValue(entityId, out linkedObject))
            {
            }
            return linkedObject;
        }
    }
}