using System;
using UnityEngine;


namespace MDG.Game.Util.Pool
{
    public class Reusable : MonoBehaviour
    {
        public event Action<GameObject> OnReuse;
        private void OnDisable()
        {
            OnReuse?.Invoke(this.gameObject);
        }
    }
}