using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StructureSchema = MdgSchema.Common.Structure;

// Move these comments to read me of dtos.
// Move all DTOs to be scriptable objects instead.
// make testing UI alot easier as well as making these specific instances much more scalalbe.
namespace MDG.DTO
{
    [Serializable]
    public class StructureConfig
    {
        public StructureSchema.StructureType structureType;
        public int constructionTime;
    }

    [Serializable]
    public class SpawnStructureConfig
    {
        public InventoryConfig inventoryConfig;
    }

    [Serializable]
    public class ClaimConfig : StructureConfig
    {
        public EntityId territoryId;
    }
}
