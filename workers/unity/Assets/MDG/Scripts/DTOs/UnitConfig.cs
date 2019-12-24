//Change this name to all come from base mdg package.
using MdgSchema.Common.Util;
using MdgSchema.Units;
using System;

namespace MDG.DTO
{
    [Serializable]
    public class UnitConfig
    {
        public UnitTypes unitType;
        public long ownerId;
        public Vector3f position;
    }
}
