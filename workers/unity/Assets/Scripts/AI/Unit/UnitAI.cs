using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MDG.Game;
using log4net;
using System.Linq;
using MDG.Common;

namespace MDG.Units
{
    public class UnitAI : MonoBehaviour

    {
        /*
        UnitVision vision;
        // Replacing this state with a controller that has access to component.
        // essentially renaming it, or could keep it same and state also as component that has it's actual state
        // and basically just wraps state around to have methods.

            
        #region BT Tasks


        // Need to design how commands for each unit will be structured as well.
        [Task]
        protected virtual bool DidReceiveNewCommand()
        {
            return state.ReceivedNewCommand;
        }

        [Task]
        //Collect and ReactToEnemy will be chained through decorations.
        protected virtual void CollectResource()
        {
            // Get posiiton of nearest resource and begin moving towards it.
            List<Interactable> nearbyResources = vision.GetVision(InteractableTypes.Resource);
            Interactable closest = nearbyResources.OrderBy((Interactable i) => Vector3.Distance(i.transform.position, transform.position)).Last();
            
            // Simplicity sake closest, later on will weigh options with avl tree that is changing based on state.
            // Also create even distrubtion of units over a collection of resources.
            Vector3.MoveTowards(transform.position, closest.transform.position, Time.deltaTime * state.Speed);      
            //Must also avoid obstacles, so likely have to implement nav mesh as well.
        }

        [Task]
        protected virtual void ReactToEnemy()
        {
            //Base adapts based on behaviour set in state.
        }

        [Task]
        bool IsDead()
        {
            return state.Health <= 0;
        }

        [Task]
        protected virtual void Die()
        {
            //Base will clear inventory
        }

        [Task]
        bool CheckNearby(string typeString = null)
        {
            InteractableTypes type;
            if (typeString == null)
            {
                return vision.HasVision();
            }
            else if (System.Enum.TryParse(typeString, out type))
            {
                return vision.HasVision(type);
            }
            return false;
        }
        #endregion
        */
    }
}