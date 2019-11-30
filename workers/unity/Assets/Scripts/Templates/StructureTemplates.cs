using Improbable;
using Improbable.Gdk.Core;
using InventorySchema = MdgSchema.Common.Inventory;
using StructureSchema = MdgSchema.Common.Structure;
using CollisionSchema = MdgSchema.Common.Collision;
using MDG.DTO;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using MDG.Common;

namespace MDG.Templates
{
    class StructureTemplates
    {
        public static EntityTemplate GetStructureTemplate(string clientWorkerId, byte[] structureArgs, Vector3f position)
        {
            string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate entityTemplate = new EntityTemplate();

            StructureConfig structureConfig = Converters.DeserializeArguments<StructureConfig>(structureArgs);

            switch (structureConfig.structureType)
            {
                case StructureSchema.StructureType.Spawning:
                    GetSpawnStructureTemplate(entityTemplate, Converters.DeserializeArguments<SpawnStructureConfig>(structureArgs), serverAttribute);
                    break;
                case StructureSchema.StructureType.Claiming:
                    GetClaimStructureTemplate(entityTemplate, Converters.DeserializeArguments<ClaimConfig>(structureArgs), serverAttribute);
                    break;
                case StructureSchema.StructureType.Trap:
                    GetTrapStructureTemplate(entityTemplate, Converters.DeserializeArguments<TrapConfig>(structureArgs), clientWorkerId, serverAttribute);
                    break;
            }

            CommonTemplates.AddRequiredSpatialComponents(entityTemplate, "Structure");
            CommonTemplates.AddRequiredGameEntityComponents(entityTemplate, position, MdgSchema.Common.GameEntityTypes.Structure);

            entityTemplate.SetComponent(new Position.Snapshot
            {
                Coords = new Coordinates(position.X, position.Y, position.Z)
            });
            entityTemplate.AddComponent(new StructureSchema.StructureMetadata.Snapshot
            {
                StructureType = structureConfig.structureType,
                ConstructionTime = structureConfig.constructionTime
            }, serverAttribute);

            entityTemplate.AddComponent(new StructureSchema.Structure.Snapshot
            {
                Constructing = false,
            }, serverAttribute);

            return entityTemplate;
        }

        private static void GetSpawnStructureTemplate(EntityTemplate template, SpawnStructureConfig structureConfig, string serverAttribute)
        {
            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                InventorySize = structureConfig.inventoryConfig.inventorySize,
                Inventory = structureConfig.inventoryConfig.itemToCost
            }, serverAttribute);
        }

        private static void GetClaimStructureTemplate(EntityTemplate template, ClaimConfig claimConfig, string serverAttribute)
        {
            template.AddComponent(new StructureSchema.ClaimStructure.Snapshot{
                TerritoryClaiming = claimConfig.territoryId
            }, serverAttribute);
        }

        private static void GetTrapStructureTemplate(EntityTemplate template, TrapConfig trapConfig, string workerId, string serverAttribute)
        {
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);

            template.AddComponent(new StructureSchema.Trap.Snapshot
            {
                TrapId = trapConfig.trapId,
                Damage = trapConfig.Damage,
                OneTimeUse = trapConfig.OneTimeUse
            }, serverAttribute);
            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                Dimensions = trapConfig.ColliderDimensions,
                IsTrigger = true
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, serverAttribute);
        }
    }

    class StructureArchtypes
    {
        public static void AddStructureArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            if (!authoritative)
            {
             //   entityManager.AddComponent<Enemy>(entity);
            }
        }
    }

}