﻿using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using System;

namespace MDG.Common.Components
{
    // Used to update UI based on click, as well as used by other systems.
    public struct Clickable : IComponentData
    {
        public bool Clicked;
        // Todo: change this to be list of clicked.
        public EntityId ClickedEntityId;
    }
}