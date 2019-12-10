using Improbable.Gdk.Subscriptions;
using MDG.Invader.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Common.MonoBehaviours
{
    // Literally just a util to avoid expanding selection grid a minimum size that wroks for all. 
    // Selection system queries this for single select then processing what are selected through multi-select
    public class ClickableMonobehaviour : MonoBehaviour
    {

        public bool SelectedThisFrame { private set; get; }
        public bool MouseOver { private set; get; }

        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)  )
            {
                SelectedThisFrame = MouseOver;
            }
        }

        private void OnMouseOver()
        {
            MouseOver = true;
        }
        
        private void OnMouseExit()
        {
            MouseOver = false;
        }
    }
}