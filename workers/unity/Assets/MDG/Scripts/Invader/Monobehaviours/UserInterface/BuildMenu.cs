using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using UnityEngine;
using static MDG.ScriptableObjects.Constants;
using ScriptableObjectStructures = MDG.ScriptableObjects.Structures;


namespace MDG.Invader.Monobehaviours.UserInterface
{
    /// <summary>
    /// Build menu is a generic User Interface monobehaviour.
    /// - It is used for build menu of unit upon being selected by a Invader.
    /// - It is used for menu of what kinda of units can spawn for Spawn Structure.
    /// - It is used for add ons later for upgrading structures if I hve that.
    /// </summary>
    public class BuildMenu : MonoBehaviour
    {
        MenuSlot[] menuSlots;

        public event Action<ShopItem> OnOptionSelected;

        public event Action<ShopItem> OnOptionConfirmed;


        public delegate bool ConfirmQuery();
        private ConfirmQuery ConfirmCallback;

        MenuSlot lastClicked;
        ResourceRequest menuSlotPrefabPromise;
        public string MenuSlotPrefabPath;
        public string MenuContentsPath;
        public void SetConfirmation(ConfirmQuery confirmationCall)
        {
            ConfirmCallback = confirmationCall;
        }

        // Images set should be injected, but watcha gonna do.
        void Start()
        {
            menuSlotPrefabPromise = Resources.LoadAsync(MenuSlotPrefabPath);
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
                Debug.Log(resources[i]);
                ShopItem shopItem = resources[i] as ShopItem;
                GameObject gameObject = Instantiate(menuSlotPrefabPromise.asset, transform) as GameObject;
                Debug.Log(gameObject.name);
                menuSlots[i] = gameObject.GetComponent<MenuSlot>();
                // This casting is not needed at this stage, just for confirmation, later on remove the swich and simply set the item.
                switch (shopItem.shopItemType)
                {
                    case ShopItemType.Buildable:
                        ScriptableObjectStructures.Structure structure = resources[i] as ScriptableObjectStructures.Structure;
                        menuSlots[i].SetItem(structure);
                        break;
                    case ShopItemType.Unit:
                        // Do later.
                        ShopUnit shopUnit = resources[i] as ShopUnit;
                        menuSlots[i].SetItem(shopUnit);
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
            }
        }

        IEnumerator ConfirmSelection()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            OnOptionConfirmed?.Invoke(lastClicked.ShopItem);
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