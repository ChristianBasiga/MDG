using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ScriptableObjectStructures = MDG.ScriptableObjects.Structures;

namespace MDG.Invader.Monobehaviours.UserInterface
{
    // Shop behaviour, should I reuse build menu and menu slot??
    [RequireComponent(typeof(Button))]
    public class MenuSlot : MonoBehaviour
    {
        public event Action<MenuSlot> OnSlotClicked;
        Button button;

        public ShopItem ShopItem { private set; get; }

        // Should handle togglign of too.
        public bool Selected { set; get; }

        [SerializeField]
        Image thumbnail;

        [SerializeField]
        Text costText;

        public void SetItem(ShopItem shopItem)
        {
            thumbnail.sprite = shopItem.Thumbnail;
            costText.text = shopItem.Cost.ToString();
            ShopItem = shopItem;
        }

        // Start is called before the first frame update
        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClicked);
        }


        void OnButtonClicked()
        {
            Selected = true;
            OnSlotClicked?.Invoke(this);
        }
    }
}