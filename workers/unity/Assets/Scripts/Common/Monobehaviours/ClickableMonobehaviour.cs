﻿using Improbable.Gdk.Subscriptions;
using MDG.Invader.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Common.MonoBehaviours
{
    // How I should I get all these? idk mannnn.
    public class ClickableMonobehaviour : MonoBehaviour
    {

        public bool SelectedThisFrame { private set; get; }
        public bool MouseOver { private set; get; }
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
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