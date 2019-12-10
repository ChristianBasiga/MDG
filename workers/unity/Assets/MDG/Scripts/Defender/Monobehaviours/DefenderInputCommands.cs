using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MDG.Common.Systems.Point;
using MDG.Common.Systems.Spawn;
using MDG.DTO;
using MDG.ScriptableObjects.Game;
using MDG.ScriptableObjects.Items;
using MdgSchema.Common.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScriptableStructures = MDG.ScriptableObjects.Structures;

namespace MDG.Defender.Monobehaviours
{
    public class DefenderInputCommands : MonoBehaviour
    {
        TrapPlacer trapPlacer;
        Shooter shooter;
        LinkedEntityComponent linkedEntityComponent;
        InputConfig inputConfig;

#pragma warning disable 649
        [SerializeField]
        LoadoutSlot[] loadoutSlots;
#pragma warning restore 649

        int selectedSlot = 0;
        // Start is called before the first frame update
        void Start()
        {
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            shooter = GetComponent<Shooter>();
            trapPlacer = GetComponent<TrapPlacer>();

         //   GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };

            // FOr now just load in all traps and set it.
            Object[] traps = Resources.LoadAll("ScriptableObjects/Traps");
            if (traps != null)
            {
                for (int i = 0; i < traps.Length; ++i)
                {
                    ScriptableStructures.Trap loadedInTrap = traps[i] as ScriptableStructures.Trap;
                    loadoutSlots[i + 1].SetItem(loadedInTrap);
                }
                loadoutSlots[0].SetItem(GetComponent<Shooter>().Weapon);
                loadoutSlots[0].Toggle(true);
            }
            else
            {
                Debug.LogError("Failed to load traps");
            }
        }

        public void Init(InputConfig inputConfig)
        {
            this.inputConfig = inputConfig;
        }

        // Update is called once per frame
        void Update()
        {

            if (inputConfig == null)
            {
                return;
            }

            for (int i = 0; i < loadoutSlots.Length; ++i)
            {
                bool selectionMade = Input.GetKeyDown((i + 1).ToString());
                if (selectionMade && i != selectedSlot)
                {
                    loadoutSlots[selectedSlot].Toggle(false);
                    loadoutSlots[i].Toggle(true);
                    selectedSlot = i;
                }
            }

            if (Input.GetButtonDown(inputConfig.LeftClickAxis))
            {
                ProcessSelection();
            }
        }


        void ProcessSelection()
        {
            LoadoutSlot selectedLoadoutSlot = loadoutSlots[selectedSlot];
            Debug.Log(selectedLoadoutSlot.SlotType);
            switch (selectedLoadoutSlot.SlotType)
            {
                case LoadoutSlot.SlotOptions.Weapon:
                    shooter.Shoot();
                    break;
                case LoadoutSlot.SlotOptions.Trap:
                    trapPlacer.TryPlaceTrap(selectedLoadoutSlot.Trap);
                    break;
                case LoadoutSlot.SlotOptions.None:
                    Debug.Log("Nothing selected");
                    break;
            }
        }

        
    }
}