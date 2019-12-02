﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MDG.ScriptableObjects.Items;
namespace MDG.Common.MonoBehaviours
{
    public class ItemSlot : MonoBehaviour
    {
        public Sprite blankSlotImage;
        Image itemImage;
        InventoryItem details;

        private void Start()
        {
            itemImage = transform.GetChild(0).GetComponent<Image>();
        }
        public void UpdateSlot(InventoryItem inventoryItem)
        {
            details = inventoryItem;
            itemImage.sprite = inventoryItem.Thumbnail;
        }

        public void ClearSlot()
        {
            itemImage.sprite = blankSlotImage;
        }
    }
}