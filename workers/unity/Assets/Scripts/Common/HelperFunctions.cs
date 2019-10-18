using Improbable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common
{
    /// <summary>
    /// Contains all general helper functions all modules can use.
    /// </summary>
    public class HelperFunctions
    {

        /// <summary>
        /// Checks if witin region of bonding 2D box
        /// </summary>
        /// <param name="center"></param>
        /// <param name="dimensions"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool IsWithinRegion(Vector3f center, Vector3f dimensions, Vector3f position)
        {
            float width = dimensions.X;
            float height = dimensions.Z;

            return (position.X <= center.X + width / 2)
                && (position.X >= center.X - width / 2)
                && (position.Z <= center.Z + height / 2)
                && (position.Z >= center.Z - height / 2);
        }

        public static bool Intersect(Vector3f center1, Vector3f dimensions1, Vector3f center2, Vector3f dimensions2)
        {
            return (
                    center1.X + dimensions1.X / 2 >= center2.X - dimensions2.X / 2
                    || center1.Z + dimensions1.Z / 2 >= center2.Z - dimensions2.Z / 2
                    || center1.X - dimensions1.X / 2 <= center2.X + dimensions2.X / 2
                    || center1.Z - dimensions1.Z / 2 <= center2.Z + dimensions2.Z / 2
                );
        }
    }

}