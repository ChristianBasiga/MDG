using System.Collections;
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
            this._default = default;
            this._world = world;
            this._workerType = workerType;
            //Then initializes own creators.

        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.HasComponent<Metadata.Component>()) return;

            Metadata.Component metaData = entity.GetComponent<Metadata.Component>();

            string pathToEntity = $"Prefabs/{_workerType}";

            //Create constants page for this later on as well.
            if (metaData.EntityType == "Player")
            {
                //Also need to add component of what kind of player it is.
                PlayerType type = entity.GetComponent<PlayerMetaData.Component>().PlayerType;


                //Authority may be applicable to unit actions, but not really. can change as needed late ron.
                var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, _world);

                if (hasAuthority)
                {
                    pathToEntity = $"{pathToEntity}/Authoritative";
                }

                pathToEntity = $"{pathToEntity}/Player";

                
            }
            else if (metaData.EntityType == "Unit")
            {

            }


            //Change to get from pool instead later on in final version of project.
            Object prefab = Resources.Load(pathToEntity);
            GameObject gameObject = Object.Instantiate(prefab) as GameObject;

            //Seems like can inject components is that better to just add or not add or to just have diff similiar prefabs?
            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject);
        }

        public void OnEntityRemoved(EntityId entityId)
        {

            //Add back to pool or whatever.
        }

    }
}