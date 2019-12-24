//Change this name to all come from base mdg package.
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System;

namespace MDG.DTO
{
    [Serializable]
    public class PlayerConfig
    {
        public GameEntityTypes playerType;
        public Vector3f position;
    }
}
