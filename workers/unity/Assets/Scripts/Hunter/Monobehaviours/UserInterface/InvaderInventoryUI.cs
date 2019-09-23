using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using MDG.ScriptableObjects;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using InventorySchema = MdgSchema.Common.Inventory;
using MDG.Factories;

namespace MDG.Hunter.Monobehaviours
{
    public class InvaderInventoryUI : MonoBehaviour
    {
        [Require] InventorySchema.InventoryReader InventoryReader;

        [SerializeField]
        Text resourceCount;
        
        InventoryItemFactory InventoryItemFactory;

        private void Start()
        {
            InventoryReader.OnInventoryUpdate += UpdateInventoryRender;
        }

        private void UpdateInventoryRender(Dictionary<int, InventorySchema.Item> inventory)
        {
            var values = inventory.Values;

            var resourceItemCount = values.Where((InventorySchema.Item item) =>
            {
                // Instead of equals 1, it should reference these item ids from static value.
                return item.Id.Equals(InventoryItemFactory.ResourceItemId);
            });

            resourceCount.text = resourceItemCount.ToString();
        }

        private void Update()
        {
            
        }
    }
}