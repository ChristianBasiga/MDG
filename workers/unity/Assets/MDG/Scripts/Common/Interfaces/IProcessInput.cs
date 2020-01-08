namespace MDG.Common.Interfaces
{
    //[RequireComponent(typeof(InputProcessorManager))]
    public interface IProcessInput
    {
        void AddToManager();
        void ProcessInput();
        void Disable();
        void Enable();
    }
}