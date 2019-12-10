using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{
    public struct CombatMetadata : IComponentData
    {
        public float attackCooldown;
        public float attackRange;

    }

    public struct CombatStats : IComponentData
    {
        public float attackCooldown;
        public float attackRange;
    }
}