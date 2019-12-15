using Improbable.Gdk.Subscriptions;
using UnityEngine;
using PointSchema = MdgSchema.Common.Point;

namespace MDG.Common.MonoBehaviours
{
    public class PointSynchonizer : MonoBehaviour
    {

        [Require] PointSchema.PointReader pointReader;
        public event System.Action<int> OnPointUpdate;
        private void Start()
        {

            pointReader.OnValueUpdate += (points) =>
            {
                OnPointUpdate?.Invoke(points);
            };
        }
    }
}
