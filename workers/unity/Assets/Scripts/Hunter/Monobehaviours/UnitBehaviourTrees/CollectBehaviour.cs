﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using MDG.Hunter.Components;
using Improbable.Gdk.Core;

namespace MDG.Hunter.Monobehaviours
{
    // Later all these deps will be injected, for now this is fine.
    // Later on use ComponentWRapper
    public class CollectBehaviour : MoveBehaviour
    {
        NavMeshAgent agent;
        [SerializeField]
        Vector3 targetDestination;
        private EntityId target;

        //Later on initialize depending on target Id.
        //Will get its dependancies from Command payload from Pending Commands
        public override void Initialize(EntityId id, CommandListener commandData)
        {
            base.Initialize(id, commandData);
            minDistance = 2.0f;
            target = commandData.TargetId;
            StartCoroutine(CommandCoroutine());
        }

        protected override void Start()
        {
            base.Start();
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
                }
                else if (!DoneExecuting())
                {
                }
                else
                {
                    FinishCommand();
                }
            }
        }

        protected override bool DoneExecuting()
        {
            return true;
        }
    }
}