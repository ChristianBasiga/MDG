using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{
    public struct CombatMetadata : IComponentData
    {
        public float attackCooldown;
    }

    public struct CombatStats : IComponentData
    {
        public float attackCooldown;
    }
}