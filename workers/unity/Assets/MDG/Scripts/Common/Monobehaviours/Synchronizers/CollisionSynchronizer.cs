using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
        // Amount before sync up with server.
        const int collisionBufferSize = 10;
        Dictionary<EntityId, CollisionSchema.CollisionPoint> collisionBuffer;

        private void Awake()
        {
            collisionBuffer = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
            
        }
        private void Start()
        {
            //StartCoroutine(SyncCollisions());
            collisionWriter.OnAuthorityUpdate += CollisionWriter_OnAuthorityUpdate;
        }

        private void CollisionWriter_OnAuthorityUpdate(Improbable.Worker.CInterop.Authority obj)
        {
            Debug.Log("authority here");
        }

        private void Update()
        {   
            collisionWriter.SendUpdate(new CollisionSchema.Collision.Update
            {
                Collisions = collisionBuffer,
                CollisionCount = collisionBuffer.Count
            });
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("on collision enter");
            if (other.gameObject.TryGetComponent(out LinkedEntityComponent linkedEntityComponent))
            {
                if (collisionBuffer.Count >= collisionBufferSize)
                {
                }

                EntityId collidedId = linkedEntityComponent.EntityId;
                CollisionSchema.CollisionPoint collisionPoint = new CollisionSchema.CollisionPoint
                {
                    CollidingWith = collidedId,
                    Distance = HelperFunctions.Vector3fFromUnityVector(other.transform.position - transform.position)
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


        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out LinkedEntityComponent linkedEntityComponent))
            {
                collisionBuffer.Remove(linkedEntityComponent.EntityId);
            }
        }
    }
}