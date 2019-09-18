using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{
    [RemoveAtEndOfTick]
    public struct PendingInventoryAddition : IComponentData
    {
        public EntityId InventoryItemId;
    }
    
}