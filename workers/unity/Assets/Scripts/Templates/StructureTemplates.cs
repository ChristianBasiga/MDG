using Improbable;
using Improbable.Gdk.Core;
using InventorySchema = MdgSchema.Common.Inventory;
using StructureSchema = MdgSchema.Common.Structure;
using MDG.DTO;

namespace MDG.Templates
{
    class StructureTemplates
    {

        public static EntityTemplate GetStructureTemplate(string clientWorkerId, byte[] structureArgs)
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
                    GetClaimStructureTempalte(entityTemplate, Converters.DeserializeArguments<ClaimConfig>(structureArgs), serverAttribute);
                break;
            }

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

        private static void GetClaimStructureTempalte(EntityTemplate template, ClaimConfig claimConfig, string serverAttribute)
        {
            template.AddComponent(new StructureSchema.ClaimStructure.Snapshot{
                TerritoryClaiming = claimConfig.territoryId
            }, serverAttribute);
        }

    }
}