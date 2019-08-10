using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.States;
using MDG.Game;
namespace MDG.Units
{
    public enum UnitBehaviourSet
    {
        Aggressive,
        Sneaky
    }
   
    // Set of flags for what it can see.
    // The detections can be reduced based on state.
    public struct UnitDetections
    {
        bool detectHunted;
        bool detectAllyUnits;
        bool detect;
    }

    // Model of Unit, may change to spatialOS entity as need be and respective component will be accessed.
    public class UnitState : AccumilativeState
    {
        public int health;
        public UnitBehaviourSet setBehaviour;
        public int experience;
        //Make it isntead of Interactable, Enemy and Resource later.
        List<Interactable> enemiesNearby;
        List<Interactable> resourcesNearby;
        // Inventory as well, but need to figur eout best way for that. For now list for POC.
        // Inventory needs to be a spatialOS component, client workers send updates
        // but single server worker should have one single place for all Unit Inventories

        //AI is accumlative, or can be seen as such in tasks, like collect.
        public UnitState(State accum) : base(accum)
        {
        }

        public void AddEnemyNearby(Interactable enemy)
        {
            enemiesNearby.Add(enemy);
        }
        public void AddResourceNearby(Interactable resource)
        {
            enemiesNearby.Add(resource);
        }

        public bool IsEnemyNearby()
        {
            return enemiesNearby.Count > 0;
        }

        public bool IsResourceNearby()
        {
            return resourcesNearby.Count > 0;
        }
    }


}