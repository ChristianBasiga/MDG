using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MDG.Hunter.Components;
using Improbable.Gdk.Core;

namespace MDG.Hunter.Monobehaviours
{
    public abstract class UnitBehaviour : MonoBehaviour
    {
        protected EntityId entityId;
        [SerializeField]
        protected bool executingCommand;

        public virtual void Initialize(EntityId id, CommandListener commandData)
        {
            this.entityId = id;
            executingCommand = true;
        }
        protected void FinishCommand()
        {
            executingCommand = false;
            Destroy(this);
        }

        protected bool HasCommand()
        {
            return executingCommand;
        }
    }
}