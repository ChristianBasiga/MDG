using Improbable;
using Improbable.Gdk.Core;
using InventorySchema = MdgSchema.Common.Inventory;
using StructureSchema = MdgSchema.Common.Structure;
using MDG.DTO;

namespace MDG.Templates
{
    class StructureTemplates
    {

        public static EntityTemplate GetStructureTemplate(string workerId, byte[] structureArgs)
        {
            string serverAttribute = UnityGameLogicConnector.WorkerType;
            EntityTemplate entityTemplate = new EntityTemplate();

            StructureConfig structureConfig = Converters.DeserializeArguments<StructureConfig>(structureArgs);

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
    }
}