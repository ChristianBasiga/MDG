using Improbable.Gdk.Core;
using MDG.Common;
using MDG.DTO;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using Unity.Entities;
using CollisionSchema = MdgSchema.Common.Collision;
using CommonSchema = MdgSchema.Common;
using PositionSchema = MdgSchema.Common.Position;
using StatSchema = MdgSchema.Common.Stats;
using WeaponSchema = MdgSchema.Common.Weapon;

namespace MDG.Templates
{
    public class WeaponTemplates
    {
        // Passing in will be weapon type and wielder type. Could generate projectiles or whatever.
        // Will take in byte array that I'll deserialize accordingly. Is this extra for the time I have? Yes.
        public static EntityTemplate GetWeaponEntityTemplate(string workerId, WeaponSchema.WeaponType weaponType, EntityId wielder,
            string prefabName, byte[] specificArguments)
        {
            string clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);

            EntityTemplate template = new EntityTemplate();
            CommonTemplates.AddRequiredSpatialComponents(template, "Weapon");
            switch (weaponType)
            {
                case WeaponSchema.WeaponType.Projectile:
                    ProjectileConfig projectileConfig = Converters.DeserializeArguments<ProjectileConfig>(specificArguments);
                    AddProjectileComponents(clientAttribute, template, wielder, prefabName, projectileConfig);
                    break;
            }

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, clientAttribute);

            template.AddComponent(new EntityRotation.Snapshot
            {
                Rotation = new Vector3f(0, 0, 0)
            }, clientAttribute);

            template.AddComponent(new WeaponSchema.Damage.Snapshot
            {
                DamageDealt = 0,
                Hits = 0
            }, clientAttribute);     

            return template;
        }

        private static void AddProjectileComponents(string clientAttribute, EntityTemplate template,
            EntityId wielder, string prefabName, ProjectileConfig projectileConfig)
        {
            string serverAttribute = UnityGameLogicConnector.WorkerType;
            CommonTemplates.AddRequiredGameEntityComponents(template, projectileConfig.StartingPosition,
                MdgSchema.Common.GameEntityTypes.Weapon, projectileConfig.ProjectileId);

            template.AddComponent(new Owner.Snapshot
            {
                OwnerId = wielder
            }, serverAttribute);
           
            template.AddComponent(new WeaponSchema.Weapon.Snapshot
            {
                BaseDamage = projectileConfig.Damage,
                Durability = projectileConfig.MaximumHits,
                WielderId = wielder,
                WeaponId = prefabName,
                WeaponType = WeaponSchema.WeaponType.Projectile
            }, serverAttribute);


            template.AddComponent(new CommonSchema.TimeLimitation.Snapshot
            {
                TimeLeft = projectileConfig.LifeTime
            }, serverAttribute);

            template.AddComponent(new StatSchema.MovementSpeed.Snapshot
            {
                LinearSpeed = projectileConfig.ProjectileSpeed,
                AngularSpeed = 0
            }, serverAttribute);

            template.AddComponent(new PositionSchema.LinearVelocity.Snapshot
            {
                Velocity = projectileConfig.LinearVelocity
            }, clientAttribute);

            template.AddComponent(new PositionSchema.AngularVelocity.Snapshot
            {
                AngularVelocity = projectileConfig.AngularVelocity
            }, clientAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Dimensions = projectileConfig.Dimensions,
                IsTrigger = true,
                Position = new Vector3f(0,0,0)
            }, serverAttribute);

           
        }
    }

    public class WeaponArchtypes
    {
        public static void AddWeaponArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            if (!authoritative)
            {

                UnityEngine.Debug.Log("Adding enemy to weapn entity");
                entityManager.AddComponent<Enemy>(entity);
            }
            else
            {
            }
        }
    }

}