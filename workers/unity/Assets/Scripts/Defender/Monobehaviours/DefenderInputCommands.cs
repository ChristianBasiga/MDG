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

        [Require] PointReader pointReader = null;

        [SerializeField]
        LoadoutSlot[] loadoutSlots;

        int selectedSlot = 0;
        // Start is called before the first frame update
        void Start()
        {
            linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            shooter = GetComponent<Shooter>();
            trapPlacer = GetComponent<TrapPlacer>();

            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };

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
            if (Input.GetAxis(inputConfig.RightClickAxis) != 0)
            {
                //TryPlaceTrap();
            }
        }


        void ProcessSelection()
        {
            LoadoutSlot selectedLoadoutSlot = loadoutSlots[selectedSlot];

            switch (selectedLoadoutSlot.SlotType)
            {
                case LoadoutSlot.SlotOptions.Weapon:
                    // More later.
                    shooter.Shoot();
                    break;
                case LoadoutSlot.SlotOptions.Trap:
                    // Slots should be interchangable so as it is not really built for that
                    // unless I store it in the slot itself. Which is feasible.

                    // Traps will be one off loadout slots due to weapon. 
                    //trapPlacer.TryPlaceTrap(traps[selectedSlot - 1]);
                    trapPlacer.TryPlaceTrap(selectedLoadoutSlot.Trap);
                    break;
            }
        }

        
    }
}