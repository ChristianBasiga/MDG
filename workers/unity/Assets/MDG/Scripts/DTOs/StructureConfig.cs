using MdgSchema.Common.Util;
using System;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.DTO
{

    [Serializable]
    public class StructureConfig
    {
        public StructureSchema.StructureType StructureType;
        public int ConstructionTime;
        public bool Constructing;
        public int Health;
        public long OwnerId;
    }

    [Serializable]
    public class InvaderStructureConfig : StructureConfig
    {
        public int WorkersRequired;
    }

    [Serializable]
    public class SpawnStructureConfig : InvaderStructureConfig
    {
        public InventoryConfig InventoryConfig;
    }

    [Serializable]
    public class ClaimConfig : StructureConfig
    {
        public long TerritoryId;
    }

    [Serializable]
    public class TrapConfig : StructureConfig
    {
        public int Damage;
        public string PrefabName;
        public Vector3f ColliderDimensions;
        public bool OneTimeUse;
    }
}
