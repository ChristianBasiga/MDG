using Improbable;
using Improbable.Gdk.Core;
using InventorySchema = MdgSchema.Common.Inventory;
using StructureSchema = MdgSchema.Common.Structure;
using MDG.DTO;

namespace MDG.Templates
{
    class StructureTemplates
    {

        public static EntityTemplate GetStructureTemplate(byte[] structureArgs)
        {
            string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate entityTemplate = new EntityTemplate();

            StructureConfig structureConfig = Converters.DeserializeArguments<StructureConfig>(structureArgs);

            switch (structureConfig.structureType)
            {
                case StructureType.Spawning:
                    GetSpawnStructureTemplate(template, Converters.DeserializeArguments<SpawnStructureConfig>(structureArgs), serverAttribute);
                break;

                case StructureType.Claiming:
                    GetClaimStructureTempalte(template, Converters.DeserializeArguments<ClaimConfig>(structureArgs), serverAttribute);
                break;
                
                default:
            }
            entityTemplate.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                InventorySize = structureConfig.inventoryConfig.inventorySize,
                Inventory = structureConfig.inventoryConfig.itemToCost
            }, serverAttribute);

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

        private static void GetSpawnStructureTemplate(Entitytempalte template, SpawnStructureConfig structureConfig, string serverAttribute)
        {
            entityTemplate.AddComponent(new InventorySchema.Inventory.Snapshot
            {
                InventorySize = structureConfig.inventoryConfig.inventorySize,
                Inventory = structureConfig.inventoryConfig.itemToCost
            }, serverAttribute);
        }

        private static void GetClaimStructureTempalte(EntityTemplate template, ClaimConfig claimConfig, string serverAttribute)
        {
            entityTemplate.AddComponent(new StructureSchema.ClaimStructure.Snapshot{
                TerritoryId = claimConfig.territoryId
            }, serverAttribute);
        }

    }
}