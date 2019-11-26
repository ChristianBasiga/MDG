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


        #region Vector Operations

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

        public static Vector3f Vector3fFromUnityVector(Vector3 unityVector)
        {
            return new Vector3f(unityVector.x, unityVector.y, unityVector.z);
        }

        public static Coordinates CoordinatesFromUnityVector(Vector3 unityVector)
        {
            return new Coordinates(unityVector.x, unityVector.y, unityVector.z);
        }

        public static float Slope(Vector3f p1, Vector3f p2)
        {
            return (p2.Z - p1.Z) / (p2.X - p1.X);
        }

        // returns dot product of normalized vectors.
        public static float DotProduct(Vector3f lhs, Vector3f rhs)
        {
            return Vector3.Dot(Normalize(lhs).ToUnityVector(), Normalize(rhs).ToUnityVector());
        }

        public static bool IsLeftOfVector(Vector3 lhs, Vector3 rhs)
        {
            float angleOfCollisionPoint = Mathf.Atan2(lhs.z, lhs.x);
            angleOfCollisionPoint -= 90.0f;
            Vector3 normal = new Vector3(Mathf.Cos(angleOfCollisionPoint), 0, Mathf.Sign(angleOfCollisionPoint));
            return Vector3.Dot(normal, rhs) < 0;
        }

        public static float Magnitude(Vector3f vector)
        {
            float sumProduct = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
            return Mathf.Sqrt(sumProduct);
        }
        public static Vector3f Normalize(Vector3f vector)
        {
            return vector / Magnitude(vector);
        }

        public static float Distance(Vector3f pos1, Vector3f pos2)
        {
            return (pos1 - pos2).ToUnityVector().magnitude;
        }

        public static bool Intersect(Vector3f center1, Vector3f dimensions1, Vector3f center2, Vector3f dimensions2)
        {
            return (
                    center1.X + dimensions1.X / 2 >= center2.X - dimensions2.X / 2
                    && center1.Z + dimensions1.Z / 2 >= center2.Z - dimensions2.Z / 2
                    && center1.X - dimensions1.X / 2 <= center2.X + dimensions2.X / 2
                    && center1.Z - dimensions1.Z / 2 <= center2.Z + dimensions2.Z / 2
                );
        }

        #endregion
       
        #region UI related helper functions.
        public static IEnumerator UpdateFill(UnityEngine.UI.Image image, float pct, float timeToUpdate = 2.2f)
        {
            if (pct == image.fillAmount)
            {
                yield return new WaitForEndOfFrame();
            }
            {
                float elapsed = 0;
                float currPercentage = image.fillAmount;

                while (elapsed < timeToUpdate)
                {
                    elapsed += Time.deltaTime;
                    image.fillAmount = Mathf.Lerp(currPercentage, pct, elapsed / timeToUpdate);
                    yield return null;
                }
                image.fillAmount = pct;
            }
        }
            #endregion
    }

}