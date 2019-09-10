using Improbable.Gdk.Subscriptions;
using MDG.Common.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// UI for each Unit.
// having multiple instead of single instance managing it might be bit much,
namespace MDG.ClientSide.UserInterface {
    public class UnitUIManager : MonoBehaviour
    {
        // GUI for signifying that this Unit has been selected.
        
        public Rect selectionRect;
        public GUIContent selectionSkin;
        // Start is called before the first frame update
        void Start()
        {
         
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}