using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace MDG.Common.Components
{
    // Rename to RenderMetadata later once I decide what data to out
    // empty component is fine. People make fucking empty interfaces.
    // maybe have mesh and material here as well.

    public enum MeshTypes
    {
        UNIT
    }
    [RemoveAtEndOfTick]
    public struct RenderPending : IComponentData
    {
        public MeshTypes meshToRender;
    }
    public struct Rendered : IComponentData
    {

    }
}