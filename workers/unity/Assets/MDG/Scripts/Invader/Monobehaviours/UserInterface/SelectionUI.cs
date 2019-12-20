using MDG.Invader.Monobehaviours.InputProcessors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Invader.Monobehaviours.UserInterface
{
    [RequireComponent(typeof(SelectionController))]
    public class SelectionUI : MonoBehaviour
    {
        private Rect selectionGridRect;
        public GUIContent selectionGridSkin;

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
            selectionGridRect.y = Screen.height - payload.startPosition.y;
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
            if (selectionGridRect != Rect.zero && selectionGridRect.width != 0 && selectionGridRect.height != 0)
            {
                GUI.Box(selectionGridRect, selectionGridSkin);
            }
        }
    }
}