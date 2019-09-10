using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace MDG.Hunter.Systems
{
    [DisableAutoCreation]
    public class EntitySelectionGroup : ComponentSystemGroup
    {
        [DisableAutoCreation]
        public class InternalSpatialOSReceiveGroup : ComponentSystemGroup
        {
        }
    }
}