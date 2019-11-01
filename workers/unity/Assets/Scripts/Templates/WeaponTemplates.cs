using Improbable;
using Improbable.Gdk.Core;
using WeaponSchema = MdgSchema.Common.Weapon;
using PositionSchema = MdgSchema.Common.Position;
using CollisionSchema = MdgSchema.Common.Collision;
using CommonSchema = MdgSchema.Common;
using MDG.DTO;
namespace MDG.Templates
{
    public class WeaponTemplates
    {
        // Passing in will be weapon type and wielder type. Could generate projectiles or whatever.
        // Will take in byte array that I'll deserialize accordingly. Is this extra for the time I have? Yes.
        public static EntityTemplate GetWeaponEntityTemplate(string workerId, WeaponSchema.WeaponType weaponType, EntityId wielder,
            byte[] specificArguments)
        {
            string clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);

            EntityTemplate template = new EntityTemplate();
            CommonTemplates.AddRequiredSpatialComponents(template, "Weapon");
            switch (weaponType)
            {
                case WeaponSchema.WeaponType.Projectile:
                    ProjectileConfig projectileConfig = Converters.DeserializeArguments<ProjectileConfig>(specificArguments);
                    AddProjectileComponents(clientAttribute, template, wielder, projectileConfig);
                    break;
            }

            template.AddComponent(new WeaponSchema.Damage.Snapshot
            {
                DamageDealt = 0,
                Hits = 0
            }, clientAttribute);

            return template;
        }

        private static void AddProjectileComponents(string clientAttribute, EntityTemplate template, 
            EntityId wielder, ProjectileConfig projectileConfig)
        {
            CommonTemplates.AddRequiredGameEntityComponents(template, projectileConfig.startingPosition,
                MdgSchema.Common.GameEntityTypes.Weapon, projectileConfig.projectileId);

            template.AddComponent(new WeaponSchema.Weapon.Snapshot
            {
                BaseDamage = projectileConfig.damage,
                Durability = projectileConfig.maximumHits,
                WielderId = wielder,
                WeaponId = projectileConfig.projectileId,
                WeaponType = WeaponSchema.WeaponType.Projectile
            }, UnityGameLogicConnector.WorkerType);

            template.AddComponent(new CommonSchema.TimeLimitation.Snapshot
            {
                TimeLeft = projectileConfig.lifeTime
            }, UnityGameLogicConnector.WorkerType);

            template.AddComponent(new PositionSchema.LinearVelocity.Snapshot
            {
                Velocity = projectileConfig.linearVelocity
            }, clientAttribute);

            template.AddComponent(new PositionSchema.AngularVelocity.Snapshot
            {
                AngularVelocity = projectileConfig.angularVelocity
            }, clientAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Dimensions= projectileConfig.dimensions,
                IsTrigger = true,
                Position = Vector3f.Zero
            }, UnityGameLogicConnector.WorkerType);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, UnityGameLogicConnector.WorkerType);
        }
    }
}