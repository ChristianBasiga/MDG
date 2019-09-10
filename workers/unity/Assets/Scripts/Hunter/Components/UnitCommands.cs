﻿using Improbable.Gdk.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace MDG.Hunter.Components
{
    //Add more to these as needed.
    public struct MoveCommand : IComponentData
    {
        public float3 destination;
    }

    // Will get positon of target each time to follow.
    public struct AttackCommand : IComponentData
    {
        public EntityId target;
    }

    // resource location won't change, so won't be checking position each frame
    public struct CollectCommand : IComponentData
    {
        public EntityId resourceId;
        public float3 destination;
    }
}