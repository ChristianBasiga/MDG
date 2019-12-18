using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG.Defender.Monobehaviours
{
    public class Loadout : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        LoadoutSlot[] loadoutSlots;
#pragma warning restore 649

        int selectedSlot = 0;


        public int LoadoutLength
        {
            get
            {
                return loadoutSlots.Length;
            }
        }

        public LoadoutSlot Selection
        {
            get
            {
                return loadoutSlots[selectedSlot];
            }
        }

        void Start()
        {
            // Key thing is order might be off.
            loadoutSlots = GetComponentsInChildren<LoadoutSlot>();
        }

        // Exrta layer of indirection not needed.
        public void SelectSlot(int i)
        {
            loadoutSlots[selectedSlot].Toggle(false);
            loadoutSlots[i].Toggle(true);
            selectedSlot = i;
        }

        public void SetSlot(int i, ScriptableObjects.Structures.Trap trap)
        {
            loadoutSlots[i].SetItem(trap);
        }

        public void SetSlot(int i, ScriptableObjects.Weapons.Weapon weapon)
        {
            loadoutSlots[i].SetItem(weapon);
        }

    }
}