﻿using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using CollisionSchema = MdgSchema.Common.Collision;

namespace MDG.Common.MonoBehaviours
{
    [RequireComponent(typeof(Collider))]
    public class CollisionSynchronizer : MonoBehaviour
    {
#pragma warning disable 649
        [Require] CollisionSchema.CollisionWriter collisionWriter;
        [Require] CollisionSchema.BoxColliderReader boxColliderReader;
#pragma warning restore 649
        Dictionary<EntityId, CollisionSchema.CollisionPoint> collisionBuffer;
        bool dirtyBit = false;
        private void Awake()
        {
            collisionBuffer = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
            
        }
        private void Update()
        {
            if (dirtyBit)
            {
                dirtyBit = false;
                CollisionSchema.Collision.Update update;
                if (boxColliderReader.Data.IsTrigger)
                {
                    update = new CollisionSchema.Collision.Update
                    {
                        Triggers = collisionBuffer,
                        TriggerCount = collisionBuffer.Count
                    };
                    if (collisionBuffer.Count > 0)
                    {
                        collisionWriter.SendTriggerHappenEvent(new CollisionSchema.CollisionEventPayload
                        {
                            CollidedWith = collisionBuffer
                        });
                    }
                }
                else
                {
                    update = new CollisionSchema.Collision.Update
                    {
                        Collisions = collisionBuffer,
                        CollisionCount = collisionBuffer.Count,

                    };

                    if (collisionBuffer.Count > 0)
                    {
                        collisionWriter.SendCollisionHappenEvent(new CollisionSchema.CollisionEventPayload
                        {
                            CollidedWith = collisionBuffer
                        });
                    }
                }
                collisionWriter.SendUpdate(update);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            UpdateCollisionBuffer(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
      //      UpdateCollisionBuffer(other.gameObject);
        }

        private void UpdateCollisionBuffer(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out LinkedEntityComponent linkedEntityComponent))
            {
                dirtyBit = true;
                EntityId collidedId = linkedEntityComponent.EntityId;
                if (linkedEntityComponent.Worker != null && linkedEntityComponent.Worker.TryGetEntity(collidedId, out Entity entity))
                {
                    EntityManager entityManager = linkedEntityComponent.World.EntityManager;
                    bool isTrigger = false;
                    if (entityManager.HasComponent<CollisionSchema.BoxCollider.Component>(entity))
                    {
                        CollisionSchema.BoxCollider.Component boxCollider = entityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(entity);
                        isTrigger = boxCollider.IsTrigger;
                    }
                    else
                    {
                    }
                    CollisionSchema.CollisionPoint collisionPoint = new CollisionSchema.CollisionPoint
                    {
                        CollidingWith = collidedId,
                        Distance = HelperFunctions.Vector3fFromUnityVector(gameObject.transform.position - transform.position),
                        IsTrigger = isTrigger
                    };
                    if (collisionBuffer.ContainsKey(collidedId))
                    {
                        collisionBuffer[collidedId] = collisionPoint;
                    }
                    else
                    {
                        collisionBuffer.Add(collidedId, collisionPoint);
                    }
                }
            }
        }


        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out LinkedEntityComponent linkedEntityComponent))
            {
                dirtyBit = true;
                collisionBuffer.Remove(linkedEntityComponent.EntityId);
            }
        }
    }
}