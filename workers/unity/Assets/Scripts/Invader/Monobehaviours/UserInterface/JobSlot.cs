using MDG.Common;
using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MDG.Invader.Monobehaviours.Structures
{
    public class JobSlot : MonoBehaviour
    {
        // So on job progress, updates fill.
        // actual completitin is when fill is complete.
        public event Action<ShopItem> OnJobCompleted;

        [SerializeField]
        Image jobProgressBar;
        [SerializeField]
        Image thumbnail;


        [SerializeField]
        Sprite defaultThumbnail;

        ShopItem jobPayload;


        public void ClearJob()
        {
            thumbnail.sprite = defaultThumbnail;
            jobProgressBar.fillAmount = 0;
        }
        public void SetJob(ShopItem shopItem)
        {
            thumbnail.sprite = shopItem.Thumbnail;
            this.jobPayload = shopItem;
            this.jobProgressBar.fillAmount = 0;
        }

        public void UpdateProgress(float pct)
        {
            StartCoroutine(HelperFunctions.UpdateFill(jobProgressBar, pct, OnProgressUpdated));
        }

        private void OnProgressUpdated(float pct)
        {
            if (pct == 1)
            {
                OnJobCompleted?.Invoke(jobPayload);
            }
        }
    }
}