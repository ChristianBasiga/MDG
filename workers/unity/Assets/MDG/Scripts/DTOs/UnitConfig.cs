//Change this name to all come from base mdg package.
using MdgSchema.Common.Util;
using MdgSchema.Units;
using System;

namespace MDG.DTO
{
    [Serializable]
    public class UnitConfig
    {
        public UnitTypes UnitType;
        public long OwnerId;
        public Vector3f Position;
    }
}
