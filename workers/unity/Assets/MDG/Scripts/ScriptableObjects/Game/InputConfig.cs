using UnityEngine;


namespace MDG.ScriptableObjects.Game
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/InputConfig")]
    public class InputConfig : ScriptableObject
    {
        public string LeftClickAxis;
        public string RightClickAxis;
        public string XMouseMovement;
        public string YMouseMovement;
        public string HorizontalAxis;
        public string VerticalAxis;
    }
}