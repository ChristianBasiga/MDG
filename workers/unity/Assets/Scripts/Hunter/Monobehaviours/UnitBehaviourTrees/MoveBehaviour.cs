using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using MDG.Hunter.Components;
using Improbable.Gdk.Core;

namespace MDG.Hunter.Monobehaviours
{
    //Later all these deps will be injected, for now this is fine.
    public class MoveBehaviour : UnitBehaviour
    {
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
            UnitMovementWriter = GetComponent<UnitMovementWriter>();
            agent = GetComponent<NavMeshAgent>();

            StartCoroutine(CommandCoroutine());
            // To save time perhaps can start moving.
            /*NavMeshPath navMeshPath = new NavMeshPath();
            
            if (!agent.CalculatePath(targetDestination, navMeshPath))
            {
                FinishCommand();
            }
            corners = navMeshPath.corners;*/
            base.Initialize(id, commandData);
        }


        IEnumerator CommandCoroutine()
        {

            yield return new WaitUntil(() => { return executingCommand; });

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
            while (executingCommand)
            {
                //yield return new WaitUntil(() => { return navMeshPath.status == NavMeshPathStatus.PathPartial || navMeshPath.status == NavMeshPathStatus.PathComplete; });
                if (IsAtLocation())
                {
                    FinishCommand();
                }
                MoveToLocation();
                yield return new WaitForEndOfFrame();
            }
            
        }


        public bool IsAtLocation()
        {
            float distance = Mathf.Sqrt(Mathf.Pow(targetDestination.x - transform.position.x, 2) + 
                Mathf.Pow(targetDestination.z - transform.position.z, 2));
            return distance <= minDistance;
        }

        public void MoveToLocation()
        {
            Vector3 directionVector = targetDestination - transform.position;
            agent.Move(directionVector * Time.deltaTime);
            UnitMovementWriter.UpdatePosition(transform.position);
        }
    }
}