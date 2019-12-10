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
    public class TriggerSynchronizer : MonoBehaviour
    {
#pragma warning disable 649
        [Require] CollisionSchema.CollisionWriter collisionWriter;
#pragma warning restore 649
        // Amount before sync up with server.
        const int collisionBufferSize = 10;
        Dictionary<EntityId, CollisionSchema.CollisionPoint> collisionBuffer;

        private void Awake()
        {
            collisionBuffer = new Dictionary<EntityId, CollisionSchema.CollisionPoint>();
            if (!GetComponent<Collider>().isTrigger)
            {
                throw new System.Exception("Trigger Synchronizer must be on a gameobject with a trigger collision");
            }
        }
        private void Start()
        {
            StartCoroutine(SyncTriggers());
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("on trigger enter");
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

        // Maybe should do every frame lol. just for consistancy.
        IEnumerator SyncTriggers()
        {
            while (gameObject.activeInHierarchy && collisionWriter.IsValid)
            {
                for (int i = 0; i < collisionBufferSize; ++i)
                {
                    yield return new WaitForEndOfFrame();
                }
                collisionWriter.SendUpdate(new CollisionSchema.Collision.Update
                {
                    Triggers = collisionBuffer
                });
            }
        }
    }
}