﻿using System.Collections.Generic;
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
using Unity.Rendering;
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

namespace MDG
{
    // Note: Refactor this to use pool later.

    /// <summary>
    /// This creates corresponding game object to entity, as well as adds any extra ECS components
    /// an entity needs. Perhaps ladder can be moved to different.
    /// </summary>
    // Use zenject to install stuff here.
    public class ClientGameObjectCreator : IEntityGameObjectCreator
    {
        public delegate void EntityChangeEventHandler(EntityId entityId);

        // For others to know when thish happens.
        public event System.Action<GameObject> OnGameObjectSpawned;
        public event EntityChangeEventHandler OnEntityAdded;
        public event EntityChangeEventHandler OnEntityDeleted;
        // Storing here prob fine actually.
        public Dictionary<EntityId,GameObject> EntityToGameObjects { private set; get; }
        // Get from pool down line.
        private Dictionary<GameEntityTypes, Dictionary<bool, GameObject>> keyToPrefabs;
        private readonly IEntityGameObjectCreator _default;
        private readonly Unity.Entities.World _world;
        private readonly string _workerType;
        private ComponentUpdateSystem ComponentUpdateSystem;
        SpawnSystems.SpawnRequestSystem spawnReqSystem;

        private int defenderPointsUsed = 0;

        LinkedEntityComponent playerLink;

        public LinkedEntityComponent PlayerLink
        {
            get
            {
                if (playerLink == null)
                {
                    Debug.Log(GameObject.FindGameObjectWithTag("Player"));
                    playerLink = GameObject.FindGameObjectWithTag("Player").GetComponent<LinkedEntityComponent>();
                }
                return playerLink;
            }
        }


        //Look into being able to add multiple custom creators and see if can do that instead.   
        //I can still do factory plan this way.

        //Make worker type an enum to parse.
        public ClientGameObjectCreator(IEntityGameObjectCreator _default, Unity.Entities.World world, string workerType)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;
            EntityToGameObjects = new Dictionary<EntityId, GameObject>();
            ComponentUpdateSystem = _world.GetExistingSystem<ComponentUpdateSystem>();
            spawnReqSystem = _world.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.HasComponent<Metadata.Component>()) return;
            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";

            Debug.Log($"creating {metaData.EntityType}");
            // Prob switch on game entity type.
            if (metaData.EntityType.Equals("Player"))
            {
                bool hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);
                string pathToPlayer = hasAuthority ? $"{pathToEntity}/Authoritative" : pathToEntity;
                if (!entity.HasComponent<GameMetadata.Component>())
                {
                    return;
                }
                GameMetadata.Component gameMetaData = entity.GetComponent<GameMetadata.Component>();
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity ecsEntity);
                GameEntityTypes type = gameMetaData.Type;
                Debug.LogError("type being made " + type);

                if (type == GameEntityTypes.Hunter)
                {
                    PlayerArchtypes.AddInvaderArchtype(worker.EntityManager, ecsEntity, hasAuthority);

                }
                else
                {

                    PlayerArchtypes.AddDefenderArchtype(worker.EntityManager, ecsEntity, hasAuthority);
                }

                if (!hasAuthority && gameMetaData.Type == GameEntityTypes.Hunter)
                {
                    Debug.LogError("I happen");
                    return;
                }
                pathToPlayer = $"{pathToPlayer}/{type.ToString()}";
                CreateEntityObject(entity, linker, pathToPlayer);
            }
            else if (metaData.EntityType.Equals("Unit"))
            {
                UnitSchema.Unit.Component unitComponent = entity.GetComponent<UnitSchema.Unit.Component>();

                bool hasAuthority = unitComponent.OwnerId.Equals(PlayerLink.EntityId);
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
                pathToEntity = $"{pathToEntity}/Resource";
                CreateEntityObject(entity, linker, pathToEntity, null, null);
            }
            else if (metaData.EntityType.Equals("GameManager"))
            {
                if (GameObject.FindGameObjectWithTag("GameManager") == null)
                {
                    pathToEntity = $"{pathToEntity}/GameManager";
                    CreateEntityObject(entity, linker, pathToEntity, null, null);
                }
            }
            else if (metaData.EntityType.Equals("Weapon"))
            {
                WeaponSchema.Weapon.Component weaponComponent = entity.GetComponent<WeaponSchema.Weapon.Component>();
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity weaponEntity);
                // Put in game manager.
                bool hasAuthority = PlayerLink.EntityId.Equals(weaponComponent.WielderId);
                WeaponArchtypes.AddWeaponArchtype(_world.EntityManager, weaponEntity, hasAuthority);
                pathToEntity = $"{pathToEntity}/Weapons/{weaponComponent.WeaponId}";

                CreateEntityObject(entity, linker, pathToEntity, null, null);
            }
            else if (metaData.EntityType.Equals("Territory"))
            {
                // Don't neccessarily need to create prefabfor this, maybe down line, but just being within area is fine for testing.
                //pathToEntity = $"{pathToEntity}/Territories/${}"
            }
            else if (metaData.EntityType.Equals("Structure"))
            {
                // add owner to structure id.
                StructureSchema.StructureMetadata.Component structureMetaData = entity.GetComponent<StructureSchema.StructureMetadata.Component>();

                // Maybe should have an ownership component, reduce copy. That's clen up later tho.
                Debug.Log("player id" + PlayerLink.EntityId);
                Debug.Log("structure owner id " + structureMetaData.OwnerId);
                bool hasAuthority = structureMetaData.OwnerId.Equals(PlayerLink.EntityId);
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
                Debug.Log("StructurePath " + structurePath);
                CreateEntityObject(entity, linker, structurePath);
            }
            else
            {
                _default.OnEntityCreated(entity, linker);
                return;
            }

            OnEntityAdded?.Invoke(entity.SpatialOSEntityId);
        }

        public void OnEntityRemoved(EntityId entityId)
        {
            _default.OnEntityRemoved(entityId);
            GameObject linkedGameObject;

            if (EntityToGameObjects.TryGetValue(entityId, out linkedGameObject))
            {
                // Perhaps custom death and / or death behaviour that plays specific animation.
                // Or both. Regardless doesn't just make it dissapear.
                if (linkedGameObject.GetComponent<HealthSynchronizer>() == null)
                {
                }
                linkedGameObject.SetActive(false);
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