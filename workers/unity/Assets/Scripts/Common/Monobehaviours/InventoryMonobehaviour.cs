using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using InventorySchema = MdgSchema.Common.Inventory;
using MDG.Factories;
namespace MDG.Common.MonoBehaviours
{
    
    public class InventoryMonobehaviour : MonoBehaviour
    {
        [Require] InventorySchema.InventoryReader InventoryReader;
        // This should be added via zenject later.
        InventoryItemFactory InventoryItemFactory;
        InventoryUI inventoryUI;
       
        // Now desiging UI, need to make a base class that this has reference to.
        // Start is called before the first frame update
        void Start()
        {
            InventoryItemFactory = new InventoryItemFactory();
            InventoryItemFactory.Initialize();

            InventoryReader.OnInventoryUpdate += UpdateUI;
        }

        private void UpdateUI(Dictionary<int, InventorySchema.Item> inventory)
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}