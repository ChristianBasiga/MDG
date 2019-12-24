using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Invader.Systems;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


namespace MDG.Common
{
    /// <summary>
    /// Contains all general helper functions all modules can use.
    /// </summary>
    public class HelperFunctions
    {
        #region ECS related operations


        // First see who is playertag
        public static GameEntityTypes PlayerType()
        {
            LinkedEntityComponent playerLink = GameObject.FindGameObjectWithTag("Player").GetComponent<LinkedEntityComponent>();
            playerLink.Worker.TryGetEntity(playerLink.EntityId, out Entity entity);
            GameMetadata.Component gameMetadata = playerLink.World.EntityManager.GetComponentData<GameMetadata.Component>(entity);
            return gameMetadata.Type;
        }


        #endregion
        #region Vector Operations


        public static Vector3f Scale(Vector3f vector3F, float scalar)
        {
            return new Vector3f(vector3F.X * scalar, vector3F.Y * scalar, vector3F.Z * scalar);
        }


        public static Vector3f Add(Vector3f lhs, Vector3f rhs)
        {
            return new Vector3f(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        public static Vector3f Subtract(Vector3f lhs, Vector3f rhs)
        {
            return new Vector3f(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static bool IsEqual(Vector3f lhs, Vector3f rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        }


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


        public static Vector3f Normalize(Vector3f vector3F)
        {
            float magnitude = Magnitude(vector3F);
            if (magnitude == 0)
            {
                return new Vector3f(0, 0, 0);
            }
            return new Vector3f(vector3F.X / magnitude, vector3F.Y / magnitude, vector3F.Z / magnitude);
        }

        public static float Distance(Vector3f lhs, Vector3f rhs)
        {
            Vector3f vector3 = Subtract(lhs, rhs);

            return HelperFunctions.Magnitude(vector3);
        }

     
        public static Vector3 Vector3fToVector3(Vector3f vector3)
        {
            return new Vector3(vector3.X, vector3.Y, vector3.Z);
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

        public static bool Intersect(Vector3f center1, Vector3f dimensions1, Vector3f center2, Vector3f dimensions2)
        {
            return (
                    center1.X + dimensions1.X / 2 >= center2.X - dimensions2.X / 2
                    && center1.Z + dimensions1.Z / 2 >= center2.Z - dimensions2.Z / 2
                    && center1.X - dimensions1.X / 2 <= center2.X + dimensions2.X / 2
                    && center1.Z - dimensions1.Z / 2 <= center2.Z + dimensions2.Z / 2
                );
        }

        public static Vector3 GetMousePosition(Camera camera)
        {
            Vector3 screenPos = Input.mousePosition;
            Ray ray = camera.ScreenPointToRay(screenPos);
            Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity);
            return hit.point;
        }

        #endregion
       

        public static string FloatToTimeStamp(float time)
        {
            // Get minutes and remaining time after remving minutes
            int minutes = (int)(time / 60);
            int seconds = (int)(time - (minutes * 60));

            string minuteText = minutes.ToString();
            if (minutes / 10 == 0)
            {
                minuteText = "0" + minuteText;
            }
            string secondText = seconds.ToString();
            if (seconds / 10 == 0)
            {
                secondText = "0" + secondText;
            }
            string timestamp = $"{minuteText}:{secondText}";
            return timestamp;
        }

        #region UI related helper functions.
        public static IEnumerator UpdateFill(UnityEngine.UI.Image image, float pct, Action<float> OnFillUpdated = null, float timeToUpdate = 2.2f, System.Func<bool> interruptCheck = null)
        {
            if (image == null)
            {
                yield break;
            }
            if (pct == image.fillAmount)
            {
                yield return new WaitForEndOfFrame();
            }
            else {
                float elapsed = 0;
                float currPercentage = image.fillAmount;

                while (elapsed < timeToUpdate)
                {
                    if (interruptCheck != null && interruptCheck())
                    {
                        image.fillAmount = pct;
                        OnFillUpdated?.Invoke(pct);
                        yield break;
                    }
                    elapsed += Time.deltaTime;
                    image.fillAmount = Mathf.Lerp(currPercentage, pct, elapsed / timeToUpdate);
                    yield return null;
                }
                image.fillAmount = pct;
            }
            OnFillUpdated?.Invoke(pct);
        }
        #endregion
    }

}