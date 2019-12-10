using MdgSchema.Common.Util;
using Unity.Entities;
namespace MDG.Invader.Components
{
    public struct RerouteComponent : IComponentData
    {
        public Vector3f destination;
        public Vector3f subDestination;
        public bool applied;

    }
}