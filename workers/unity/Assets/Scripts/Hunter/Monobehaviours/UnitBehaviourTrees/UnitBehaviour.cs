using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MDG.Hunter.Components;
using Improbable.Gdk.Core;

namespace MDG.Hunter.Monobehaviours
{
    public abstract class UnitBehaviour : MonoBehaviour
    {
        // Due to linked entity component this isn't needed anymore remove later.
        protected EntityId entityId;
        [SerializeField]
        protected bool executingCommand;

        public virtual void Initialize(EntityId id, CommandListener commandData)
        {
            this.entityId = id;
            executingCommand = true;
        }

        protected abstract IEnumerator CommandCoroutine();
        protected void FinishCommand()
        {
            executingCommand = false;
            Destroy(this);
        }

        protected abstract bool DoneExecuting();

        protected bool HasCommand()
        {
            return executingCommand;
        }
    }
}