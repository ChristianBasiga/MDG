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

#pragma warning disable 649
        [SerializeField]
        Image jobProgressBar;
        [SerializeField]
        Image thumbnail;
        [SerializeField]
        Sprite defaultThumbnail;
#pragma warning restore 649

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
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HelperFunctions.UpdateFill(jobProgressBar, pct, OnProgressUpdated, 1, () => !this.gameObject.activeInHierarchy));
            }
            else
            {
                jobProgressBar.fillAmount = pct;
            }
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