﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using Improbable;
using Player.Metadata;
namespace MDG
{
    //Will have reference to all creators and factories such as default, player, unit and spawner.
    public class CustomObjectCreation : IEntityGameObjectCreator
    {

        private readonly IEntityGameObjectCreator _default;
        private readonly World _world;
        private readonly string _workerType;

        //Look into being able to add multiple custom creators and see if can do that instead.   
        //I can still do factory plan this way.

        //Make worker type an enum to parse.
        public CustomObjectCreation(IEntityGameObjectCreator _default, World world, string workerType)
        {
            this._default = _default;
            this._world = world;
            this._workerType = workerType;
            //Then initializes own creators.

        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.HasComponent<Metadata.Component>()) return;

            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";
            Debug.Log(metaData.EntityType);
            //Create constants page for this later on as well.
            if (metaData.EntityType.Equals("Player"))
            {
                //Also need to add component of what kind of player it is.

                
                PlayerType type = entity.GetComponent<PlayerMetaData.Component>().PlayerType;

                Debug.Log(type);
                try
                {
                    //Only client one running.


                    //Authority may be applicable to unit actions, but not really. can change as needed late ron.
                    var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);

                    if (hasAuthority)
                        pathToEntity = $"{pathToEntity}/Authoritative";

                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);

                }

                pathToEntity = $"{pathToEntity}/{type.ToString()}";

                
            }
            else
            {
                Debug.Log("Using default");
                _default.OnEntityCreated(entity, linker);
                return;

            }


            Debug.Log(pathToEntity);
            //Change to get from pool instead later on in final version of project.
            Object prefab = Resources.Load(pathToEntity);
            GameObject gameObject = Object.Instantiate(prefab) as GameObject;
            gameObject.name = $"{prefab.name}(SpatialOS: {entity.SpatialOSEntityId}, Worker: {_workerType})";

            //Seems like can inject components is that better to just add or not add or to just have diff similiar prefabs?
            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
        }

        public void OnEntityRemoved(EntityId entityId)
        {

            //Add back to pool or whatever.
            _default.OnEntityRemoved(entityId);
        }

    }
}