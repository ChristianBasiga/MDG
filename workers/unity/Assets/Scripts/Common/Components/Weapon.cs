using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components.Weapon
{
    [RemoveAtEndOfTick]
    public struct UpdateDurability: IComponentData
    {
        public int amountToDecrease;
    }
}
