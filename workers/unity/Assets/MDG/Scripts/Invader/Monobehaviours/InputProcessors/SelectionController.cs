using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MDG.Invader.Monobehaviours.InputProcessors
{
    public class SelectionController : MonoBehaviour, IProcessInput
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
        private Vector3 startSelection;


        private void Start()
        {
            AddToManager();
        }
        Vector3 GetSelectionScale()
        {
            Vector3 selectionSize = new Vector3(Input.mousePosition.x - startSelection.x, startSelection.y - Input.mousePosition.y);
            return selectionSize;
        }
     
        public void ProcessInput()
        {
            // Later on replace this with input config as well, not priority though.
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

        public void AddToManager()
        {
            GetComponent<InputProcessorManager>().AddInputProcessor(this);
        }

        public void Disable()
        {
        }

        public void Enable()
        {
        }
    }
}