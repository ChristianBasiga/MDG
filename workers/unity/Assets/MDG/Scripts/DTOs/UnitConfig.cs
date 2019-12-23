﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Change this name to all come from base mdg package.
using MdgSchema.Units;
using System;
using MdgSchema.Common;
using MdgSchema.Common.Util;

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