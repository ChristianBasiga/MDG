using UnityEngine;



namespace MDG.ScriptableObjects.World
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.WorldPath + "/Territory")]
    public class Territory : ScriptableObject
    {
        public Vector3 Position;
        public string Name;
        public int PointGain;
        public float ParticipationRadius;
        public float ClaimTime;
    }
}