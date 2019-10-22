using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace MDG.Invader.Systems
{
    /// <summary>
    /// Checks for collision events on units.
    /// Reroute them by altering velocity for the moment or appling angular velocity.
    /// Angular velocity is in itself applied to velocity. Could do same way did ocean travel, should look into alternatives.
    /// I have linear velocity, I'll derive orientation from that, Then update velocity accordingl. I'd have to map out alternate path to same destination.
    /// // Incase I go with this, how should I implement.
    /// </summary>
    public class UnitRerouteSystem : MonoBehaviour
    {
        // So first I'll prob need a reroute component, having finalDestination as field, then to get next desintation, find first angle whose
        // whose next step in velocity is no longer having collision.

        // So to do that, I want to rotate vector, until I get a vector where exists no scalar that can result in a point
        // that is within the bounds of the box collider of the collision


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}