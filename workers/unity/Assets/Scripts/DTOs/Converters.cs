using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using MDG.Common;
using MDG.ScriptableObjects.Weapons;
using StructureSchema = MdgSchema.Common.Structure;
namespace MDG.DTO
{
    public class Converters
    {
        public static T DeserializeArguments<T>(byte[] serializedArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.Write(serializedArguments, 0, serializedArguments.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }


        //So my serialization is fucking up.
        public static byte[] SerializeArguments<T>(T arguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, arguments);
                return memoryStream.ToArray();
            }
        }

        // Ideally I COULD just serialize the scriptable object.
        // down road will, but for now this is fine,
        public static ProjectileConfig ProjectileToProjectileConfig(Projectile projectile)
        {
            ProjectileConfig projectileConfig = new ProjectileConfig
            {
                projectileId = projectile.WeaponId,
                dimensions = HelperFunctions.Vector3fFromUnityVector(projectile.Dimensions),
                lifeTime = projectile.LifeTime,
                maximumHits = projectile.Durability,
                damage = projectile.Damage,
            };
            return projectileConfig;
        }

        public static WeaponMetadata WeaponToWeaponMetadata(Weapon weapon)
        {
            return new WeaponMetadata
            {
                attackCooldown = weapon.AttackCooldown,
                weaponType = weapon.weaponType
            };
        }

        public static StructureConfig StructureToStructureConfig(Structure structure)
        {
            switch (structure.StructureType)
            {
                case StructureSchema.Spawning:
                    
                    Dictionary<int, Item> inventory = new Dictionary<int, Item>();
                    for (int i = 0; i < structure.Options.Length; ++i)
                    {
                        inventory[i] = new Item 
                        {
                            Id = i,
                            // replace metadata with actual bit partitioned metadata. For now this is fine.
                            // cost checks should be done on client side before server side checks.
                            ItemMetadata = 0
                        };
                    }
                    InventoryConfig inventoryConfig = new InventoryConfig
                    {
                        itemToCost = inventory,
                        inventory = inventory.Length
                    };
                    
                    SpawnStructureConfig spawnConfig = new SpawnStructureConfig
                    {
                        structureType = structure.StructureType,
                        constructionTime = structure.ConstructionTime,
                        inventoryConfig = inventoryConfig
                    };

                    return spawnConfig;
                case StructureSchema.Claiming:
                    // Add other stuff.
                    ClaimConfig claimConfig = new ClaimConfig
                    {
                         structureType = structure.StructureType,
                        constructionTime = structure.ConstructionTime,
                    };
                break;
            }
        }
    }
}