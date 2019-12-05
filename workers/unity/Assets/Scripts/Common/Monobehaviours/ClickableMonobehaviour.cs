using Improbable.Gdk.Subscriptions;
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
        bool mouseOver;
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log("mouse over" + mouseOver);
            if (Input.GetMouseButtonDown(0))
            {
                SelectedThisFrame = mouseOver;
            }
            Debug.Log("selected this frame " + SelectedThisFrame);
        }

        private void OnMouseDown()
        {
            Debug.Log("mouse down");
        }

        private void OnMouseOver()
        {
            Debug.Log("Mouse over");
            mouseOver = true;
        }
        
        private void OnMouseExit()
        {
            Debug.Log("Mouse exit");
            mouseOver = false;
        }
    }
}