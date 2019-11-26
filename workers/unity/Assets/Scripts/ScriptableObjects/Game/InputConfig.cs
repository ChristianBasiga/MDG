using Improbable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.ScriptableObjects.Game
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/InputConfig")]
    public class InputConfig : ScriptableObject
    {
        public string LeftClickAxis;
        public string RightClickAxis;
        public string HorizontalAxis;
        public string VerticalAxis;
    }
}