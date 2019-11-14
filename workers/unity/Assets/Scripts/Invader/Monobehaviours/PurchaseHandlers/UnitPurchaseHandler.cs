using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.ScriptableObjects.Items;
using SpawnSystems = MDG.Common.Systems.Spawn;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using Improbable.Gdk.Core;
using MDG.DTO;

namespace MDG.Invader.Monobehaviours
{
    public class UnitPurchaseHandler : PurchaseHandler
    {
        SpawnSystems.SpawnRequestSystem spawnRequestSystem;
        EntityId purchaser;
        
        // Update is called once per frame
        void Update()
        {

        }
        public bool HandlePurchase(ShopItem shopItem, ShopBehaviour shopBehaviour)
        {
            if (shopItem.shopItemType != ScriptableObjects.Constants.ShopItemType.Unit)
            {
                return false;
            }
            ShopUnit shopUnit = shopItem as ShopUnit;
            UnitConfig unitConfig = new UnitConfig
            {
                owner_id = purchaser.Id,
                unitType = shopUnit.UnitType
            };
            
            // Before / after or on same chain should start the job of constructing.
            // Two different timers ticking for same thing is kind of sketch. Prob better to start build job, then
            // build job triggers spawn request. Basically how I diagrammed it before, I'm just writing nonsense right now 
            // what am i doing with my life. I mean I COULD use this code for defender minion purchases and that will be fine.
            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Unit,
                // Could access entity transform, but synced so its fine like this.
                Position = HelperFunctions.Vector3fFromUnityVector(shopBehaviour.transform.position),
                Count = 1,
            }, OnUnitSpawned, Converters.SerializeArguments<UnitConfig>(unitConfig), null, shopUnit.ConstructTime);
            return true;
        }


        public void OnUnitSpawned(EntityId entityId)
        {
        }

        public void Handshake(LinkedEntityComponent linkedEntityComponent)
        {
            spawnRequestSystem = linkedEntityComponent.World.GetExistingSystem<SpawnSystems.SpawnRequestSystem>();
            purchaser = linkedEntityComponent.EntityId;
        }
    }
}   