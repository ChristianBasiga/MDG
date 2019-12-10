using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StructureSchema = MdgSchema.Common.Structure;
using Improbable.Gdk.Core;
using Improbable;
using MdgSchema.Common.Util;

namespace MDG.DTO
{

    // Tbh what I'm doing is going from scriptable object, to dtos, to bytes, to dtos on server side
    // to apply config. But config IS essentially the scriptable object minus sprite, etc. Actually with that 
    // in mind, this is fine. Configs have only what's needed to create template.
    [Serializable]
    public class StructureConfig
    {
        public StructureSchema.StructureType structureType;
        public int constructionTime;
        public bool constructing;
        public int health;
        public long ownerId;
    }

    [Serializable]
    public class InvaderStructureConfig : StructureConfig
    {
        public int WorkersRequired;
    }

    [Serializable]
    public class SpawnStructureConfig : InvaderStructureConfig
    {
        public InventoryConfig inventoryConfig;
    }

    [Serializable]
    public class ClaimConfig : StructureConfig
    {
        public EntityId territoryId;
    }

    [Serializable]
    public class TrapConfig : StructureConfig
    {
        public int Damage;
        public string prefabName;
        public Vector3f ColliderDimensions;
        public bool OneTimeUse;
    }
}
