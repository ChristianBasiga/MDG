using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Change this name to all come from base mdg package.
using MdgSchema.Units;
using System;
using MdgSchema.Common;
using Improbable;

namespace MDG.DTO
{
    [Serializable]
    public class UnitConfig
    {
        public UnitTypes unitType;
        public long owner_id;
        public Vector3f position;
    }
}
