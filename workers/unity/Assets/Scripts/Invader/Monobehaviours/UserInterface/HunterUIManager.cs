using MDG.Invader.Monobehaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.ClientSide.UserInterface
{
    public class HunterUIManager : MonoBehaviour
    {
        private Rect selectionGridRect;
        public GUIContent selectionGridSkin;
        // Start is called before the first frame update
        void Start()
        {
            SelectionController selectionController = GetComponent<SelectionController>();
            selectionController.OnSelectionStart += SpawnSelectionGrid;
            selectionController.OnSelection += UpdateSelectionGrid;
            selectionController.OnSelectionEnd += DespawnSelectionGrid;
        }

        public void SpawnSelectionGrid(SelectionController.SelectionPayload payload)
        {
            selectionGridRect.xMin = payload.startPosition.x;
            selectionGridRect.y = Screen.height -  payload.startPosition.y;
        }

        public void UpdateSelectionGrid(SelectionController.SelectionPayload payload)
        {
            selectionGridRect.width = payload.scale.x;
            selectionGridRect.height = payload.scale.y;
        }
        public void DespawnSelectionGrid(SelectionController.SelectionPayload payload)
        {
            selectionGridRect = Rect.zero;
        }

        private void OnGUI()
        {
            if (selectionGridRect != Rect.zero && selectionGridRect.width != 0 && selectionGridRect.height != 0) {
                GUI.Box(selectionGridRect, selectionGridSkin);
            }
        }

        
    }
}