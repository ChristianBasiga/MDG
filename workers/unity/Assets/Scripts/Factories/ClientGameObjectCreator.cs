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

namespace MDG
{

    // This needs to be updated to do multiple things.
    // 

    /// <summary>
    /// This creates corresponding game object to entity, as well as adds any extra ECS components
    /// an entity needs. Perhaps ladder can be moved to different.
    /// </summary>
    // Use zenject to install stuff here.
    public class ClientGameObjectCreator : IEntityGameObjectCreator
    {
        #region Prefab Mappings
        // type, and id to get specific name, later on will be set via injection
        private Dictionary<WeaponSchema.WeaponType, Dictionary<int, string>> weaponPrefabNames = new Dictionary<WeaponSchema.WeaponType, Dictionary<int, string>>
        {
            {
                WeaponSchema.WeaponType.Projectile, new Dictionary<int, string>{
                    { 1, "Bullet"}
                }
            }
        };
        #endregion


        public delegate void EntityChangeEventHandler(EntityId entityId);

        // For others to know when thish happens.
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
        // This actually shouldn't be here for player stuff but prior in selecting role.
        private List<Coordinates> defenderStartingPoints;
        private List<Vector3f> initialInvaderUnitPositions;

        //Look into being able to add multiple custom creators and see if can do that instead.   
        //I can still do factory plan this way.

        //Make worker type an enum to parse.
        public ClientGameObjectCreator(IEntityGameObjectCreator _default, Unity.Entities.World world, string workerType,
            GameScriptableObjects.GameConfig gameConfig)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;

            if (gameConfig != null)
            {
                this.defenderStartingPoints = new List<Coordinates>();
                this.initialInvaderUnitPositions = new List<Vector3f>();
                foreach (Vector3 pos in gameConfig.DefenderSpawnPoints)
                {
                    this.defenderStartingPoints.Add(HelperFunctions.CoordinatesFromUnityVector(pos));
                }

                foreach (Vector3 pos in gameConfig.InvaderUnitSpawnPoints)
                {
                    this.initialInvaderUnitPositions.Add(HelperFunctions.Vector3fFromUnityVector(pos));
                }
            }
    
            EntityToGameObjects = new Dictionary<EntityId, GameObject>();
            ComponentUpdateSystem = _world.GetExistingSystem<ComponentUpdateSystem>();
            spawnReqSystem = _world.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.HasComponent<Metadata.Component>()) return;
            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";
            var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);

            Debug.Log($"creating {metaData.EntityType}");
            if (metaData.EntityType.Equals("Player"))
            {
                string pathToPlayer = pathToEntity;
                if (!entity.HasComponent<GameMetadata.Component>())
                {
                    return;
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

                if (hasAuthority)
                {
                 

                    pathToPlayer = $"{pathToPlayer}/Authoritative";

                    if (GameObject.FindGameObjectWithTag("MainCamera"))
                    {
                        GameObject.FindGameObjectWithTag("MainCamera").SetActive(false);
                    }
                    if (type == GameEntityTypes.Hunter)
                    {
                            for (int i = 0; i < initialInvaderUnitPositions.Count; ++i)
                        {

                            UnitConfig unitConfig = new UnitConfig
                            {
                                ownerId = entity.SpatialOSEntityId.Id,
                                position = initialInvaderUnitPositions[i],
                                unitType = UnitTypes.WORKER
                            };


                            spawnReqSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
                            {
                                TypeToSpawn = GameEntityTypes.Unit,
                                Position = initialInvaderUnitPositions[i],
                                // Deperecate typeId, should all be serialized args for stuff like this.
                                TypeId = (int)UnitTypes.WORKER,
                                
                            }, null, Converters.SerializeArguments<UnitConfig>(unitConfig));
                        }
                        /* systems not being added during here. Hmm.
                        spawnReqSystem.World.GetOrCreateSystem<InvaderSystems.SelectionSystem>();
                        spawnReqSystem.World.GetOrCreateSystem<InvaderSystems.CommandGiveSystem>();
                        spawnReqSystem.World.GetOrCreateSystem<InvaderSystems.CommandUpdateSystem>();
                        */
                    }
                    else
                    {
                        // Add Defender specific systems.
                    }

                }
                else if (gameMetaData.Type == GameEntityTypes.Hunter)
                {
                    return;
                }
                else
                {

                }
                pathToPlayer = $"{pathToPlayer}/{type.ToString()}";
                GameObject created = CreateEntityObject(entity, linker, pathToPlayer, null, null);
                Vector3 startingPoint = defenderStartingPoints[defenderPointsUsed].ToUnityVector();
                created.transform.position = startingPoint;
                Object prefab = Resources.Load($"{pathToEntity}/FakeClientInput");
                GameObject clientFaker = Object.Instantiate(prefab) as GameObject;
            }
            else if (metaData.EntityType.Equals("Unit"))
            {
                Entity unitEntity;
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                if (worker.TryGetEntity(entity.SpatialOSEntityId, out unitEntity))
                {
                    UnitSchema.Unit.Component unitComponent = entity.GetComponent<UnitSchema.Unit.Component>();
                    Templates.UnitArchtypes.AddUnitArchtype(worker.EntityManager, unitEntity, hasAuthority, unitComponent.Type);
                }
                pathToEntity = hasAuthority ? $"{pathToEntity}/Authoritative" : pathToEntity;
                pathToEntity = $"{pathToEntity}/Unit";

                GameObject gameObject = CreateEntityObject(entity, linker, pathToEntity, null, null);
                gameObject.tag = "Unit";
                gameObject.name = $"{gameObject.name} {(hasAuthority? "authoritative" : "")}";

            }
            else if (metaData.EntityType.Equals("Resource"))
            {
                pathToEntity = $"{pathToEntity}/Resource";
                GameObject created = CreateEntityObject(entity, linker, pathToEntity, null, null);
                created.tag = "Resource";
            }
            else if (metaData.EntityType.Equals("GameManager"))
            {
                pathToEntity = $"{pathToEntity}/GameManager";
                GameObject created = CreateEntityObject(entity, linker, pathToEntity, null, null);
            }
            else if (metaData.EntityType.Equals("Weapon"))
            {
                WeaponSchema.Weapon.Component weaponComponent = entity.GetComponent<WeaponSchema.Weapon.Component>();
                WorkerSystem worker = _world.GetExistingSystem<WorkerSystem>();
                worker.TryGetEntity(entity.SpatialOSEntityId, out Entity weaponEntity);
                WeaponArchtypes.AddWeaponArchtype(_world.EntityManager, weaponEntity, hasAuthority);
                string prefabName = weaponPrefabNames[weaponComponent.WeaponType][weaponComponent.WeaponId];
                pathToEntity = $"{pathToEntity}/Weapons/{prefabName}";
                GameObject created = CreateEntityObject(entity, linker, pathToEntity, null, null);
                created.tag = weaponComponent.  WeaponType.ToString();
            }
            else if (metaData.EntityType.Equals("Territory"))
            {
                // Don't neccessarily need to create prefabfor this, maybe down line, but just being within area is fine for testing.
                //pathToEntity = $"{pathToEntity}/Territories/${}"
            }
            else
            {
                _default.OnEntityCreated(entity, linker);
                return;
            }

            OnEntityAdded?.Invoke(entity.SpatialOSEntityId);
        }

        // Also needs to know about this when Units are deleted. Easieset is to just run qury.
        // This might end up doing too much, but this could have ref to respective HUD
        // then once one of these happens, checks if what was removed / added was unit and update accordingly.
        // Feels like spaghetti tho.
        public void OnEntityRemoved(EntityId entityId)
        {
            _default.OnEntityRemoved(entityId);
            GameObject linkedGameObject;

            if (EntityToGameObjects.TryGetValue(entityId, out linkedGameObject))
            {
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
            //Seems like can inject components is that better to just add or not add or to just have diff similiar prefabs?
            if (ecsComponents != null)
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, ecsComponents);
            }
            else
            {
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
            }
            EntityToGameObjects[entity.SpatialOSEntityId] = gameObject;
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