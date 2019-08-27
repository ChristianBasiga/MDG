using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Common;
using Improbable.Gdk.Core;
using Improbable;
using Unity.Collections;

namespace MDG.Hunter.Components {

    

    public enum UnitBehaviourSet
    {
        Aggressive,
        Sneaky
    }

   
    // Could simply set a unit spawner to spawn it, and let it go one ata
    /*
    public struct UnitSpawner: IComponentData
    {
        public List<Coordinates> positions;
        public int amountToSpawn;
    }*/

    [RemoveAtEndOfTick]
    public struct InitialPosition: IComponentData
    {
        public Coordinates coordinates;
    }

    //All Unit entities also have whats in their vision.
    //Vision and AI are two different systems though.
    //This may be server component spatial instead.
    // maybe.
    public struct UnitComponent : IComponentData
    {
        public UnitBehaviourSet BehaviourSet;
        //Width, Height, Depth
        public Vector3 LineOfSight;
    }
}