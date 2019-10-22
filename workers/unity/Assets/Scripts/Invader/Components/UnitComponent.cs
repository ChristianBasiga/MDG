using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Common;
using Improbable.Gdk.Core;
using Improbable;
using Unity.Collections;
using MdgSchema.Common;

namespace MDG.Invader.Components {

    

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


    //Add more to later.
    public struct EnemyComponent: IComponentData
    {
        GameEntityTypes enemyType;
    }
}