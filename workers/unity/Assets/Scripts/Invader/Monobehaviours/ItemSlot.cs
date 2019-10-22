using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MDG.ScriptableObjects;
namespace MDG.Common.MonoBehaviours
{
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField]
        Sprite blankSlotImage;
        Image itemImage;
        InventoryItem details;

        private void Start()
        {
            itemImage = transform.GetChild(0).GetComponent<Image>();
        }
        public void UpdateSlot(InventoryItem inventoryItem)
        {
            details = inventoryItem;
            itemImage.sprite = inventoryItem.ArtWork;
        }

        public void ClearSlot()
        {
            itemImage.sprite = blankSlotImage;
        }
    }
}