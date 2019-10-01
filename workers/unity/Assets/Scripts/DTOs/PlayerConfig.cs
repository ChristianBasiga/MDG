using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Change this name to all come from base mdg package.
using MdgSchema.Player;
using System;
using MdgSchema.Common;

namespace MDG.DTO
{
    [Serializable]
    public class PlayerConfig
    {
        public GameEntityTypes playerType;
    }
}
