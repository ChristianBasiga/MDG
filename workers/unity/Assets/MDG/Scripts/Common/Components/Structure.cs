using Unity.Entities;


namespace MDG.Common.Components.Structure
{
    #region Components for structures used only in server side for structure monitor system
    public struct RunningJobComponent : IComponentData
    {
        public float JobProgress;
        public float EstimatedJobCompletion;
        public int JobId;
    }

    public struct BuildingComponent : IComponentData
    {
        public int BuildProgress;
        public int EstimatedBuildCompletion;
    }
    #endregion
}