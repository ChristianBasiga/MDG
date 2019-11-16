using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDG.Common.MonoBehaviours.Shopping;
using MDG.ScriptableObjects.Items;

namespace MDG.Invader.Monobehaviours.Structures
{
    [RequireComponent(typeof(ShopBehaviour))]
    public class UnitSpawnerStructure : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            ShopBehaviour shopBehaviour = GetComponent<ShopBehaviour>();
            shopBehaviour.OnPurchaseItem += StartSpawnJob;
        }

        private void StartSpawnJob(ScriptableObjects.Items.ShopItem shopItem, GameObject purchaser)
        {
            if (shopItem.shopItemType != ScriptableObjects.Constants.ShopItemType.Unit)
            {
                return;
            }

            ShopUnit shopUnit = shopItem as ShopUnit;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}