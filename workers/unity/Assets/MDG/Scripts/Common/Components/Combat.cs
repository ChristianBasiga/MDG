using Improbable.Gdk.Core;
using Unity.Entities;

namespace MDG.Common.Components
{

    // This should really be spatial component tbh.
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