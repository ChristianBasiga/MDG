using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.ScriptableObjects.Structures
{
    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.StructurePath + "/Trap")]
    public class Trap : ScriptableObject
    {
        public Sprite Thumbnail;
        public string PrefabPath;
        public int Cost;
        public int SetupTime;
    }
}
