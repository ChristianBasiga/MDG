using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using MDG.Hunter.Commands;
using Improbable.Gdk.Core;
using Unity.Mathematics;

namespace MDG.Hunter.Components
{
    [RemoveAtEndOfTick]
    public struct Selection : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float3 Scale;
    }
}