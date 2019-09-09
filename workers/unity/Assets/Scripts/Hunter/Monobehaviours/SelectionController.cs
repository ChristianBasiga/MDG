using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MDG.Hunter.Monobehaviours
{
    public class SelectionController : MonoBehaviour
    {
        public struct SelectionPayload
        {
            public Vector3 startPosition;
            public Vector3 endPosition;
            public Vector3 scale;
        }

        public delegate void SelectionEventHandler(SelectionPayload selectionPayload);
        public event SelectionEventHandler OnSelectionStart;
        public event SelectionEventHandler OnSelection;
        public event SelectionEventHandler OnSelectionEnd;
        [SerializeField]
        private Vector3 startSelection;
        // Add require to selection component.
        // Start is called before the first frame update
        void Start()
        {

        }

        Vector3 GetSelectionScale()
        {
            Vector3 selectionSize = new Vector3(Input.mousePosition.x - startSelection.x, startSelection.y - Input.mousePosition.y);
            return selectionSize;
        }
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                startSelection = Input.mousePosition;
                OnSelectionStart?.Invoke(new SelectionPayload { startPosition = startSelection });
            }
            else if (Input.GetMouseButton(0))
            {
                OnSelection?.Invoke(new SelectionPayload { startPosition = Input.mousePosition, scale = GetSelectionScale() });
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnSelectionEnd?.Invoke(new SelectionPayload { startPosition = startSelection, scale = GetSelectionScale(), endPosition = Input.mousePosition });
            }
        }
    }
}