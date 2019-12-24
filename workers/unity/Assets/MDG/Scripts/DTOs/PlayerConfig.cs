//Change this name to all come from base mdg package.
using MdgSchema.Common;
using MdgSchema.Common.Util;
using System;

namespace MDG.DTO
{
    [Serializable]
    public class PlayerConfig
    {
        public GameEntityTypes PlayerType;
        public string UserName;
        public Vector3f Position;
    }
}
