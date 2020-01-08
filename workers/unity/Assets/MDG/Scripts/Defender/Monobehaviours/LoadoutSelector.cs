using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using MDG.ScriptableObjects.Game;
using System.Collections;
using UnityEngine;
using ScriptableStructures = MDG.ScriptableObjects.Structures;

namespace MDG.Defender.Monobehaviours
{
    public class LoadoutSelector : MonoBehaviour, IProcessInput
    {
        TrapPlacer trapPlacer;
        Shooter shooter;
        InputConfig inputConfig;

        Loadout loadout;
        Loadout Loadout
        {
            get
            {
                if (loadout == null)
                {
                    GameObject go = GameObject.Find("Loadout");
                    if (go != null)
                        loadout = GameObject.Find("Loadout").GetComponent<Loadout>();
                }
                return loadout;
            }
        } 

        // Start is called before the first frame update
        void Start()
        {
            shooter = GetComponent<Shooter>();
            trapPlacer = GetComponent<TrapPlacer>();
            AddToManager();
            StartCoroutine(LoadLoadout());
           
        }

        IEnumerator LoadLoadout()
        {
            yield return new WaitUntil(() => Loadout != null);
            // Where it gets loadout needs to be set elsewhere. How sets loadout will be different. These loaded in stuff is options.
            // but that will be aler.
            Object[] traps = Resources.LoadAll("ScriptableObjects/Traps");
            if (traps != null)
            {
                loadout.SetSlot(0, GetComponent<Shooter>().Weapon);
                loadout.SelectSlot(0);
                for (int i = 0; i < traps.Length; ++i)
                {
                    ScriptableStructures.Trap loadedInTrap = traps[i] as ScriptableStructures.Trap;
                    loadout.SetSlot(i + 1, loadedInTrap);
                }
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

      
        public void AddToManager()
        {
            GetComponent<InputProcessorManager>().AddInputProcessor(this);
        }

        public void ProcessInput()
        {
            if (inputConfig == null)
            {
                return;
            }

            for (int i = 0; loadout != null && i < loadout.LoadoutLength; ++i)
            {
                bool selectionMade = Input.GetKeyDown((i + 1).ToString());
                if (selectionMade)
                {
                    loadout.SelectSlot(i);  
                }
            }
            if (Input.GetButtonDown(inputConfig.LeftClickAxis))
            {
                ProcessSelection();
            }
        }

        void ProcessSelection()
        {
            LoadoutSlot selectedLoadoutSlot = loadout.Selection;
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

        public void Disable()
        {
            
        }

        public void Enable()
        {
            
        }
    }
}