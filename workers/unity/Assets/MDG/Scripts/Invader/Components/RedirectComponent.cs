using MdgSchema.Common.Util;
using Unity.Entities;
namespace MDG.Invader.Components
{
    public struct RerouteComponent : IComponentData
    {
        public Vector3f Destination;
        public Vector3f SubDestination;
        public int FramesPassed;
        public bool Applied;
    }
}