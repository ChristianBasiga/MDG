using Improbable.Gdk.Core;
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
        // So I can write into it, initially but not more than that??
        [Require] CollisionSchema.CollisionWriter collisionWriter;
#pragma warning restore 649
        Dictionary<EntityId, CollisionSchema.CollisionPoint> collisionBuffer;

        private void Awake()
        {
            collisionBuffer = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
            
        }
        private void Update()
        {   
            collisionWriter.SendUpdate(new CollisionSchema.Collision.Update
            {
                Collisions = collisionBuffer,
                CollisionCount = collisionBuffer.Count,
            });

            if (collisionBuffer.Count > 0)
            {
                collisionWriter.SendCollisionHappenEvent(new CollisionSchema.CollisionEventPayload
                {
                    CollidedWith = collisionBuffer
                });
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out LinkedEntityComponent linkedEntityComponent))
            {
                EntityId collidedId = linkedEntityComponent.EntityId;

                if (linkedEntityComponent.Worker.TryGetEntity(collidedId, out Entity entity))
                {
                    Debug.Log("collided with " + other.name);
                    EntityManager entityManager = linkedEntityComponent.World.EntityManager;
                    bool isTrigger = false;
                    if (entityManager.HasComponent<CollisionSchema.BoxCollider.Component>(entity))
                    {
                        CollisionSchema.BoxCollider.Component boxCollider = entityManager.GetComponentData<CollisionSchema.BoxCollider.Component>(entity);
                        isTrigger = boxCollider.IsTrigger;
                    }
                    else
                    {
                        Debug.Log("has no box colider component");
                    }
                    CollisionSchema.CollisionPoint collisionPoint = new CollisionSchema.CollisionPoint
                    {
                        CollidingWith = collidedId,
                        Distance = HelperFunctions.Vector3fFromUnityVector(other.transform.position - transform.position),
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
                collisionBuffer.Remove(linkedEntityComponent.EntityId);
            }
        }
    }
}