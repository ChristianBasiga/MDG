using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Defender.Components {
   
    // Temp component for adding points, and triggering animation.
    [RemoveAtEndOfTick]
    public struct Disarmed: IComponentData
    {
        public EntityId unitThatDisarmed;
    }

    // Tempo component for triggering animation.
    [RemoveAtEndOfTick]
    public struct Placed: IComponentData
    {
        public int trapId;
    }



}