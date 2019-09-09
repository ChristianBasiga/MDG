using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using MDG.Hunter.Components;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable;

namespace MDG.Hunter.Monobehaviours
{
    //Later all these deps will be injected, for now this is fine.
    public class MoveBehaviour : UnitBehaviour
    {
        // Remove navmesg agent and implment own later, now I cannot fucking waste time on more ground work / reinventing wheel.
        NavMeshAgent agent;
        NavMeshObstacle obstacle;
        Vector3 targetDestination;
        UnitMovementWriter UnitMovementWriter;
        //Later on initialize depending on target Id.
        protected float minDistance = 0.001f;
        Vector3[] corners;
        int cornersTravelled = 0;
        float speed = 10;
        //Will get its dependancies from Command payload from Pending Commands
        public override void Initialize(EntityId id, CommandListener commandData)
        {
            targetDestination = commandData.TargetPosition;
           // targetDestination.y = transform.position.y;
            base.Initialize(id, commandData);
        }

        protected virtual void Start()
        {
            UnitMovementWriter = GetComponent<UnitMovementWriter>();
            agent = GetComponent<NavMeshAgent>();
            StartCoroutine(CommandCoroutine());
        }


        protected override IEnumerator CommandCoroutine()
        {


            // To save time perhaps can start moving.
            NavMeshPath navMeshPath = new NavMeshPath();
            /*
            if (!agent.CalculatePath( targetDestination, navMeshPath))
            {
                FinishCommand();
            }*/
            //agent.enabled = false;
            //int cornerCount = 0;
            //obstacle.enabled = true;
            yield return new WaitForEndOfFrame();
            while (this.enabled)
            {
                yield return new WaitUntil(() => { return executingCommand; });
                //yield return new WaitUntil(() => { return navMeshPath.status == NavMeshPathStatus.PathPartial || navMeshPath.status == NavMeshPathStatus.PathComplete; });
                if (DoneExecuting())
                {
                    FinishCommand();
                }
                MoveToLocation();
                yield return new WaitForEndOfFrame();
            }
            
        }


        protected override bool DoneExecuting()
        {
            float distance = Mathf.Sqrt(Mathf.Pow(targetDestination.x - transform.position.x, 2) + 
                Mathf.Pow(targetDestination.z - transform.position.z, 2));
            return distance <= minDistance;
        }

        public void MoveToLocation()
        {
            Vector3 directionVector = targetDestination - transform.position;
            //agent.Move(directionVector * Time.deltaTime);
            transform.position += directionVector * Time.deltaTime;
            UnitMovementWriter.UpdatePosition(transform.position);
        }
    }
}