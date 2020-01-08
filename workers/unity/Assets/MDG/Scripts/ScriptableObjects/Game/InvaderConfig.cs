using UnityEngine;


namespace MDG.ScriptableObjects.Game
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.GamePath + "/InvaderConfig")]
    public class InvaderConfig : ScriptableObject
    {
        public float CameraPanSpeed;
        public float MaxZoom;
        public float MinZoom;
        public float ScrollSpeed;
        public Vector2 PanningBorder;
        public Vector2 PanningBounds;
    }
}