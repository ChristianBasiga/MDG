using Improbable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.ScriptableObjects.Game
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/DefenderConfig")]
    public class DefenderConfig : ScriptableObject
    {
        public float MouseSensitivty;
        public float CameraMoveSpeed;
        public float MovementSpeed;
    }
}