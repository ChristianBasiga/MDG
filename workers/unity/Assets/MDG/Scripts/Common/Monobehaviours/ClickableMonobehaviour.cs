using UnityEngine;

namespace MDG.Common.MonoBehaviours
{
    // Literally just a util to avoid expanding selection grid a minimum size that wroks for all. 
    // Selection system queries this for single select then processing what are selected through multi-select
    public class ClickableMonobehaviour : MonoBehaviour
    {

        public bool SelectedThisFrame { private set; get; }
        public bool MouseOver { private set; get; }

        private void Update()
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