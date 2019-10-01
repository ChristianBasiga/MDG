using System.Collections;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using MDG.Hunter.Components;
using MdgSchema.Common;
using UnityEngine;


namespace MDG.Hunter.Monobehaviours
{
    //Instea dof inheriting composition ideal for this, but it's fine.
    //I'll separate it into data or change how I'm executing commands completely later
    public class AttackBehaviour : MoveBehaviour
    {
        private EntityId target;
        private List<long> damageRequestIds;
        private bool enemyStillInRange;
        private bool enemyStillAlive;

        // I will turn these into component and corresponding command system group later on, for POC this is fine.

        public override void Initialize(EntityId id, CommandListener commandData)
        {
            base.Initialize(id, commandData);
            enemyStillInRange = true;
            enemyStillAlive = true;
            target = commandData.TargetId;  
            damageRequestIds = new List<long>();
            minDistance = 5.0f;
        }
        protected override IEnumerator CommandCoroutine()
        {
            while (this.enabled)
            {
                yield return new WaitUntil(() => { return executingCommand; });
                yield return new WaitForEndOfFrame();
                if (!base.DoneExecuting())
                {
                    MoveToLocation();
                    yield return new WaitForEndOfFrame();
                }
                else if (!DoneExecuting())
                {
                    LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
                   
                    if (!enemyStillInRange)
                    {
                        WorkerSystem workerSystem = linkedEntityComponent.Worker;
                        Unity.Entities.Entity targetEntity;
                        if (workerSystem.TryGetEntity(target, out targetEntity))
                        {
                            Vector3 newDestination = workerSystem.EntityManager.GetComponentData<Position.Component>(targetEntity).Coords.ToUnityVector();
                            // Start movement again.
                            //base.Initialize(linkedEntityComponent.EntityId, new CommandListener { TargetPosition = newDestination });
                        }
                    }
                    else// if (damageRequestIds.Count == 0)
                    {
                        Debug.LogError("Attacking");
                        CommandSystem commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();

                        //This 100% is doing too much right now, but its fine, will rearrange later.
                        // test with just one.
                        damageRequestIds.Add(commandSystem.SendCommand(new Stats.DamageEntity.Request { TargetEntityId = linkedEntityComponent.EntityId, Payload = new DamageRequest { Damage = 1, ToDamage = target } }));
                        // This will be sending damage requests
                        // In the future
                    }
                }
                else
                {
                    FinishCommand();
                }
            }
        }

        protected override bool DoneExecuting()
        {
            //Check if target has died by querying any damage responses.
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            CommandSystem commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();

            WorkerSystem workerSystem = linkedEntityComponent.Worker;

            List<long> requestsResolved = new List<long>();
            foreach (long requestId in damageRequestIds)
            {
                var damageResponses = commandSystem.GetResponse<Stats.DamageEntity.ReceivedResponse>(requestId);
                // As we may have sent a shit ton of damage requests.
                for (int i = 0; i < damageResponses.Count; ++i)
                {
                    ref readonly var damageResponse = ref damageResponses[i];

                    switch (damageResponse.StatusCode)
                    {

                        case StatusCode.Success:
                            Debug.LogError("here too ");
                            if (damageResponse.ResponsePayload.Value.DidDie)
                            {
                                Debug.LogError("As well as here");
                                // Then we're done running this command, clear previous requests as we don't care about those responses anymore.
                                damageRequestIds.Clear();
                                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request {
                                    EntityId = damageResponse.RequestPayload.ToDamage
                                });

                                // Make request to delete entity, mayhaps a different system twill handle that, this takes response to simply indicate that done running this command.
                                return true;
                            }
                            requestsResolved.Add(requestId);

                            break;
                        case StatusCode.PermissionDenied:
                            Debug.LogError("Permission denied");
                            break;
                        case StatusCode.Timeout:
                            Debug.LogError("timed out");
                            break;
                        default:
                            Debug.LogError("Failed to damage");
                            break;
                    }
                    Debug.LogError(damageResponse.Message);
                   
                }
            }
            foreach(long requestId in requestsResolved)
            {
                damageRequestIds.Remove(requestId);
            }
            return false;
        }


        // This trigger is attack range.
        public void OnTriggerEnter(Collider other)
        {
            LinkedEntityComponent linkedEntityComponent = other.gameObject.GetComponent<LinkedEntityComponent>();
            if (linkedEntityComponent)
            {

                EntityId collidedWith = linkedEntityComponent.EntityId;

                if (collidedWith.Equals(target))
                {
                    enemyStillInRange = true;
                    Debug.LogError("Still in range and alive so continue attack.");
                }
            }
        }

        // This trigger is attack range.
        public void OnTriggerStay(Collider other)
        {
            Debug.LogError("I happen for sure");
            LinkedEntityComponent linkedEntityComponent = other.gameObject.GetComponent<LinkedEntityComponent>();
            if (linkedEntityComponent)
            {

                EntityId collidedWith = linkedEntityComponent.EntityId;

                if (collidedWith.Equals(target))
                {
                    enemyStillInRange = true;
                    Debug.LogError("Still in range and alive so continue attack.");
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            LinkedEntityComponent linkedEntityComponent = other.gameObject.GetComponent<LinkedEntityComponent>();
            if (linkedEntityComponent)
            {

                EntityId collidedWith = linkedEntityComponent.EntityId;

                if (collidedWith.Equals(target))
                {
                    Debug.LogError("Target left range");
                    enemyStillInRange = false;
                }
            }
        }
    }
}