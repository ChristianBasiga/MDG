using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Factories
{
    /// <summary>
    /// Ideally I do server logic processing stuff withut need of gameobjects.
    /// </summary>
    public class ServerGameObjectCreator : IEntityGameObjectCreator
    {

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            throw new System.NotImplementedException();
        }

        public void OnEntityRemoved(EntityId entityId)
        {
            throw new System.NotImplementedException();
        }
    }
}