using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;
using MDG.Game;
using log4net;
namespace MDG.Units
{
    public class UnitAI : MonoBehaviour
    {

        UnitVision vision;
        UnitState state;
        List<Interactable> nearby;

        void Start()
        {
            vision.OnQueryMatch += UpdateNearbyState;
        }

        void UpdateNearbyState(Interactable i)
        {
            switch (i.type)
            {
                case InteractableTypes.Enemy:
                    state.AddEnemyNearby(i);
                break;

                case InteractableTypes.Resourcee:
                    state.AddResourceNearby(i);
                break;

                default:
                    return;
            }
            //If didn't hit any cases this will remain false or set by different task.
            state.UpdatedThisFrame = true;
        }

        #region BT Tasks
        [Task]
        //Collect and ReactToEnemy will be chained through decorations.
        protected virtual void CollectResource()
        {
            //Base adds to inventory.
        }

        [Task]
        protected virtual void ReactToEnemy()
        {
            //Base adapts based on behaviour set in state.
        }

        [Task]
        bool IsDead()
        {
            return state.health <= 0;
        }

        [Task]
        protected virtual void Die()
        {
            //Base will clear inventory
        }

        [Task]
        void CheckNearby(string[] checking)
        {
            List<InteractableTypes> query = new List<InteractableTypes>();

            foreach(string typeString in checking)
            {
                InteractableTypes type;
                if (System.Enum.TryParse(typeString, out type))
                {
                    query.Add(type);
                }
                else
                {
                    //Replace with log4net once merged from logging branch.
                    Debug.Log($"{typeString} is not an Interactable Type");  
                    Task.current.Fail();
                }
            }
            vision.GetSight(query);
            Task.current.Succeed();
        }

        [Task]
        bool CheckNearbyEnemy()
        {
            return state.UpdatedThisFrame && state.IsEnemyNearby();
        }

        [Task]
        bool CheckNearbyResource()
        {
            return state.UpdatedThisFrame && state.IsResourceNearby();
        }

        #endregion
    }
}