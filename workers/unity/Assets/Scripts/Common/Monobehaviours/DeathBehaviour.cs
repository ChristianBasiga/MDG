using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.MonoBehaviours
{

    public abstract class DeathBehaviour : MonoBehaviour
    {
        // Death will usually cause a respawn, there are mono stuff want to do so this is minaly for that.
        public abstract void Die<T>(byte[] deathParams);
    }
}