using MdgSchema.Common;
using System;
using System.Collections;
using System.Collections.Generic;
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