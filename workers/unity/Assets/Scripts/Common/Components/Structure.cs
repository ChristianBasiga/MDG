using Improbable.Gdk.Core;
using MdgSchema.Common;
using Unity.Entities;

namespace MDG.Common.Components.Structure
{

    // Include enum of all job types accordingly.
    // Actually this should be stored in dictionary somewhere. Add to datastructures folder.
    enum SpawnStructureJobTypes
    {

    }

    // Progress components for keeping track of action of structure and updating UI and other clients.
    public struct RunningJobComponent : IComponentData
    {
        public float jobProgress;
        public float estimatedJobCompletion;
        public int jobType;
    }

    public struct BuildingComponent : IComponentData
    {
        public float buildProgress;
        public float estimatedBuildCompletion;
    }

    public struct ClaimingJob : IComponentData
    {
        public EntityId territoryId;
    }

    public struct SpawningJob : IComponentData
    {
        public GameEntityTypes typeSpawning;
    }
}