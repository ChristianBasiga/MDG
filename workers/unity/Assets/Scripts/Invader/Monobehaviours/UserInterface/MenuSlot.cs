using MDG.ScriptableObjects.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ScriptableObjectStructures = MDG.ScriptableObjects.Structures;

namespace MDG.Invader.Monobehaviours.UserInterface
{
    [RequireComponent(typeof(Button))]
    public class MenuSlot : MonoBehaviour
    {
        public event Action<ShopItem> OnSlotClicked;
        Button button;

        [SerializeField]
        Image thumbnail;
        ScriptableObjectStructures.Structure structure;

        public void SetItem(ScriptableObjectStructures.Structure structure)
        {
            thumbnail.sprite = structure.Thumbnail;
            this.structure = structure;
        }

        // Start is called before the first frame update
        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClicked);
        }


        void OnButtonClicked()
        {
            OnSlotClicked?.Invoke(structure);
        }
    }
}