using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StructureSchema = MdgSchema.Common.Structure;

// Factory should have all of these scriptable objects.
namespace MDG.ScriptableObjects.Structures
{

    [CreateAssetMenu(menuName = Constants.RootMenuPath + "/" + Constants.StructurePath + "/Structure")]
    public class Structure : ScriptableObject
    {
        public StructureSchema.StructureType StructureType;
        public float ConstructionTime;
        public int MaximumConcurrentJobs;
        public int MaxJobsQueued;
        // Monobehaviour should load these in and populate them.
        // For tempalte itself I only need the index and item info.
        public List<ShopItem> options;
    }
}