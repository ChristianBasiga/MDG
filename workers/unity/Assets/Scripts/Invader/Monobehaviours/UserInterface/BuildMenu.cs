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

        public event Action<ShopItem> OnOptionConfirmed;


        public delegate bool ConfirmQuery();
        private ConfirmQuery ConfirmCallback;

        MenuSlot lastClicked;
        ResourceRequest menuSlotPrefabPromise;

        public string MenuContentsPath;


        public void SetConfirmation(ConfirmQuery confirmationCall)
        {
            ConfirmCallback = confirmationCall;
        }

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
                        break;
                    case ShopItemType.Unit:
                        // Do later.
                        break;
                }
                menuSlots[i].OnSlotClicked += OnSlotClicked ;
            }
        }

        void OnSlotClicked(MenuSlot menuSlot)
        {
            lastClicked = menuSlot;
            OnOptionSelected?.Invoke(menuSlot.ShopItem);
            if (ConfirmCallback == null)
            {
                StartCoroutine(ConfirmSelection());
                OnOptionConfirmed?.Invoke(lastClicked.ShopItem);
            }
        }

        IEnumerator ConfirmSelection()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

        }

        private void Update()
        {
            if (lastClicked != null && lastClicked.Selected)
            {
                if (ConfirmCallback != null && ConfirmCallback())
                {
                    lastClicked.Selected = false;
                    OnOptionConfirmed?.Invoke(lastClicked.ShopItem);
                    lastClicked = null;
                }
            }
        }
    }
}