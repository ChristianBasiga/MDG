using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static MDG.ScriptableObjects.Constants;
using ScriptableObjectStructures = MDG.ScriptableObjects.Structures;


namespace MDG.Invader.Monobehaviours.UserInterface
{
    // Ideally spawn structure also uses this.
    public class BuildMenu : MonoBehaviour
    {
        MenuSlot[] menuSlots;

        public event Action<ShopItem> OnOptionSelected;
        ResourceRequest menuSlotPrefabPromise;

        public string MenuContentsPath;

        // Images set should be injected, but watcha gonna do.
        void Start()
        {
            menuSlotPrefabPromise = Resources.LoadAsync("UserInterface/MenuSlot");
            menuSlotPrefabPromise.completed += MenuSlotPrefabPromise_completed;
        }

        private void MenuSlotPrefabPromise_completed(AsyncOperation obj)
        {
            LoadMenuContents();
        }

        void LoadMenuContents()
        {
            object[] resources = Resources.LoadAll(MenuContentsPath);
            int length = resources.Length;
            menuSlots = new MenuSlot[length];
            for (int i = 0; i < length; ++i)
            {
                ShopItem shopItem = resources[i] as ShopItem;
                GameObject gameObject = Instantiate(menuSlotPrefabPromise.asset, transform) as GameObject;
                menuSlots[i] = gameObject.GetComponent<MenuSlot>();
                switch (shopItem.shopItemType)
                {
                    case ShopItemType.Buildable:
                        ScriptableObjectStructures.Structure structure = resources[i] as ScriptableObjectStructures.Structure;
                        menuSlots[i].SetItem(structure);
                        menuSlots[i].gameObject.SetActive(false);
                        break;
                    case ShopItemType.Unit:
                        // Do later.
                        break;
                }
                menuSlots[i].OnSlotClicked += OnSlotClicked ;
            }
        }

        void OnSlotClicked(ShopItem shopItem)
        {
            OnOptionSelected?.Invoke(shopItem);
        }
    }
}