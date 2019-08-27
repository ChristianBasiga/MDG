﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Improbable.Worker.CInterop.Query;
using Improbable.Gdk.Core.Commands;
using System.Linq;
//Perhaps make a group

namespace MDG.Common.Systems
{
    /// <summary>
    /// This system runs on server side and acts on all entities.
    /// It sends event to client side CollisionHandler system that receives the event.
    /// </summary>
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class CollisionDetectionSystem : ComponentSystem
    {
        WorkerSystem workerSystem;
        Dictionary<long, EntityId> queryIdToEntityId;
        Dictionary<EntityId, List<EntityId>> entityIdToCollisions;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            queryIdToEntityId = new Dictionary<long, EntityId>();
            entityIdToCollisions = new Dictionary<EntityId, List<EntityId>>();
        }

        protected override void OnUpdate()
        {

            #region Sending queries to check for collisions.
            Entities.WithAllReadOnly<SpatialEntityId, EntityCollider.Component>().ForEach((ref SpatialEntityId spatialEntityId, ref EntityCollider.Component collider) =>
           {
               // So send query.
               EntityQuery entityQuery = new EntityQuery
               {
                   Constraint = new SphereConstraint(collider.Position.X, collider.Position.Y, collider.Position.Z, collider.Radius)
               };
               long requestId = commandSystem.SendCommand(new WorldCommands.EntityQuery.Request
               {
                   EntityQuery = entityQuery,
               });
               queryIdToEntityId[requestId] = spatialEntityId.EntityId;
           });
            #endregion

            #region Check results of queries to map entityId to collisions
            if (queryIdToEntityId.Count > 0)
            {
                List<long> toRemove = new List<long>();
                foreach (long requestId in queryIdToEntityId.Keys) {
                    var responsePayload = commandSystem.GetResponse<WorldCommands.EntityQuery.ReceivedResponse>(requestId);
                    for (int i = 0; i < responsePayload.Count; ++i)
                    {
                        var response = responsePayload[i];
                        switch (response.StatusCode)
                        {
                            case Improbable.Worker.CInterop.StatusCode.Success:
                                Debug.LogError("Collided");
                                entityIdToCollisions[queryIdToEntityId[requestId]] = response.Result.Keys.ToList();
                                // I guess this is why usually do latter
                                toRemove.Add(requestId);

                                break;
                            case Improbable.Worker.CInterop.StatusCode.Timeout:
                                //request again.
                                break;
                        }
                    }
                }
                //Removes all request ids resolved.
                foreach (long idResolved in toRemove)
                {
                    queryIdToEntityId.Remove(idResolved);
                }
            }
            #endregion

            #region Invoke events on collisions happened
            if (entityIdToCollisions.Count > 0)
            {
                // Could prob store to make more efficient.
                Unity.Entities.EntityQuery entityQuery = GetEntityQuery(ComponentType.ReadOnly<EntityCollider.ComponentAuthority>(), ComponentType.ReadOnly<EntityCollider.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>());
                Entities.With(entityQuery).ForEach((ref EntityCollider.Component collider, ref SpatialEntityId spatialEntityId) =>
                {
                    List<EntityId> collisions;
                    if (entityIdToCollisions.TryGetValue(spatialEntityId.EntityId, out collisions))
                    {
                        componentUpdateSystem.SendEvent(new EntityCollider.OnCollision.Event(new CollisionEventPayload
                        {

                            CollidedWith = collisions,
                            ColliderType = collider.ColliderType
                        }), spatialEntityId.EntityId);
                    }
                });
            }
            #endregion
        }
    }
}