using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using StructureSchema = MdgSchema.Common.Structure;
using CollisionSchema = MdgSchema.Common.Collision;

using MDG.Common;
using MDG.Common.Systems.Stat;

namespace MDG.Defender.Monobehaviours.Traps
{
    public interface ITrap
    {       
       void ProcessTrapTriggered(List<EntityId> enemyIds);
    }

    public class TrapBehaviour : MonoBehaviour
    {
        [Require] CollisionSchema.CollisionReader collisionReader = null;
        [Require] StructureSchema.StructureReader structureReader = null;
        StructureSchema.StructureMetadata.Component structureMetadata;
        bool onCoolDown = false;
        bool processingTrigger = false;
        // Start is called before the first frame update
        void Start()
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            structureReader.OnJobCompleteEvent += OnJobComplete;
            collisionReader.OnTriggersUpdate += OnTrapCollision;
            linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Unity.Entities.Entity entity);
            structureMetadata = linkedEntityComponent.World.EntityManager.GetComponentData<StructureSchema.StructureMetadata.Component>(entity);
        }

        private void OnTrapCollision(Dictionary<EntityId, CollisionSchema.CollisionPoint> collisions)
        {
            if (processingTrigger) return;
            processingTrigger = true;
            if (!onCoolDown)
            {
                HandleEnemyCollisions(collisions.Keys);
            }
        }

        private async void HandleEnemyCollisions(Dictionary<EntityId, CollisionSchema.CollisionPoint>.KeyCollection collisionIds)
        {
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            EntityManager entityManager = linkedEntityComponent.World.EntityManager;
            Debug.Log("Handle enemy collisions is called");
            List<EntityId> enemyIds = await Task.Run(() =>
            {
               // As refetching cache multiple times for checking if has enemy component may incur I / O making it async.
               return collisionIds.Where((EntityId collisionId) =>
                {
                    return (linkedEntityComponent.Worker.TryGetEntity(collisionId, out Entity entity) &&
                    entityManager.HasComponent<Enemy>(entity));
                }).ToList();
            });

            if (enemyIds.Count > 0)
            {
                UnityEngine.Debug.Log("ran into enemies");
                GetComponent<ITrap>().ProcessTrapTriggered(enemyIds);
                //Go on cooldown.
                CommandSystem commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();
                commandSystem.SendCommand(new StructureSchema.Structure.StartJob.Request
                {
                    TargetEntityId = linkedEntityComponent.EntityId,
                    Payload = new StructureSchema.JobRequestPayload
                    {
                        EstimatedJobCompletion = structureMetadata.ConstructionTime,
                    }
                });
                onCoolDown = true;
            }
            processingTrigger = false;
        }

        private void OnJobComplete(StructureSchema.JobCompleteEventPayload obj)
        {
            Debug.Log("Job ever complete??");
            onCoolDown = false;
        }
    }
}