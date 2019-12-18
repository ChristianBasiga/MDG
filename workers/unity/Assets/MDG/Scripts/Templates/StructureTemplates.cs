using Improbable;
using Improbable.Gdk.Core;
using InventorySchema = MdgSchema.Common.Inventory;
using StructureSchema = MdgSchema.Common.Structure;
using CollisionSchema = MdgSchema.Common.Collision;
using MDG.DTO;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using MDG.Common;
using MdgSchema.Common.Stats;
using MdgSchema.Common.Util;
using MdgSchema.Common;

namespace MDG.Templates
{
    class StructureTemplates
    {
        public static EntityTemplate GetStructureTemplate(string clientWorkerId, byte[] structureArgs, Vector3f position)
        {
            string serverAttribute = UnityGameLogicConnector.WorkerType;
            string clientAttribute = EntityTemplate.GetWorkerAccessAttribute(clientWorkerId);

            EntityTemplate entityTemplate = new EntityTemplate();

            StructureConfig structureConfig = Converters.DeserializeArguments<StructureConfig>(structureArgs);

            switch (structureConfig.structureType)
            {
                case StructureSchema.StructureType.Spawning:
                    GetSpawnStructureTemplate(entityTemplate, Converters.DeserializeArguments<SpawnStructureConfig>(structureArgs), clientAttribute, serverAttribute);
                    break;
                case StructureSchema.StructureType.Claiming:
                    GetClaimStructureTemplate(entityTemplate, Converters.DeserializeArguments<ClaimConfig>(structureArgs), serverAttribute);
                    break;
                case StructureSchema.StructureType.Trap:
                    GetTrapStructureTemplate(entityTemplate, Converters.DeserializeArguments<TrapConfig>(structureArgs), clientAttribute, serverAttribute);
                    break;
            }

            CommonTemplates.AddRequiredSpatialComponents(entityTemplate, "Structure");
            CommonTemplates.AddRequiredGameEntityComponents(entityTemplate, position, MdgSchema.Common.GameEntityTypes.Structure);

            entityTemplate.AddComponent(new Owner.Snapshot
            {
                OwnerId = new EntityId(structureConfig.ownerId)
            }, serverAttribute);

            entityTemplate.AddComponent(new EntityRotation.Snapshot
            {
                Rotation = new Vector3f(0,0,0)
            }, clientAttribute);



            entityTemplate.SetComponent(new Position.Snapshot
            {
                Coords = new Coordinates(position.X, position.Y, position.Z)
            });
            entityTemplate.AddComponent(new StructureSchema.StructureMetadata.Snapshot
            {
                StructureType = structureConfig.structureType,
                ConstructionTime = structureConfig.constructionTime,
                OwnerId = new EntityId(structureConfig.ownerId)
            }, serverAttribute);

            entityTemplate.AddComponent(new StructureSchema.Structure.Snapshot
            {
                Constructing = structureConfig.constructing,
            }, serverAttribute);

            return entityTemplate;
        }

        private static void GetSpawnStructureTemplate(EntityTemplate template, SpawnStructureConfig structureConfig, string clientAttribute, string serverAttribute)
        {
            template.AddComponent(new Stats.Snapshot
            {
                Health = structureConfig.health
            }, serverAttribute);
            template.AddComponent(new StatsMetadata.Snapshot
            {
                Health = structureConfig.health
            }, serverAttribute);

            template.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                InventorySize = 10,
                Inventory = new System.Collections.Generic.Dictionary<int, InventorySchema.Item>()
            }, serverAttribute);


            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                IsTrigger = false
            }, serverAttribute);    

            template.AddComponent(new CollisionSchema.Collision.Snapshot
            {
                Collisions = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>(),
                Triggers = new System.Collections.Generic.Dictionary<EntityId, CollisionSchema.CollisionPoint>()
            }, clientAttribute);
        }

        private static void GetClaimStructureTemplate(EntityTemplate template, ClaimConfig claimConfig, string serverAttribute)
        {
            template.AddComponent(new Stats.Snapshot
            {
                Health = claimConfig.health
            }, serverAttribute);
            template.AddComponent(new StatsMetadata.Snapshot
            {
                Health = claimConfig.health
            }, serverAttribute);

            template.AddComponent(new CollisionSchema.BoxCollider.Snapshot
            {
                IsTrigger = false
            }, serverAttribute);

            template.AddComponent(new StructureSchema.ClaimStructure.Snapshot{
                TerritoryClaiming = new EntityId(claimConfig.territoryId)
            }, serverAttribute);

            // Could add collisions here for structure.
        }

        private static void GetTrapStructureTemplate(EntityTemplate template, TrapConfig trapConfig, string clientAttribute, string serverAttribute)
        {
            template.AddComponent(new StructureSchema.Trap.Snapshot
            {
                Damage = trapConfig.Damage,
                OneTimeUse = trapConfig.OneTimeUse,
                PrefabName = trapConfig.prefabName
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
            }, clientAttribute);
        }
    }

    class StructureArchtypes
    {
        public static void AddStructureArchtype(EntityManager entityManager, Entity entity, bool authoritative)
        {
            if (!authoritative)
            {
               entityManager.AddComponent<Enemy>(entity);
            }
        }
    }

}