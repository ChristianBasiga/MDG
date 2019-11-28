using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours;
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
        Shooter shooter;
        InputConfig inputConfig;

        [Require] PointReader pointReader = null;

        [SerializeField]
        LoadoutSlot[] loadoutSlots;

        int selectedSlot = 0;
        ScriptableStructures.Trap[] traps;
        // Start is called before the first frame update
        void Start()
        {
            shooter = GetComponent<Shooter>();
            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };

            // FOr now just load in all traps and set it.
            Object[] traps = Resources.LoadAll("ScriptableObjects/Traps");
            if (traps != null)
            {
                this.traps = new ScriptableStructures.Trap[traps.Length]; 
                for (int i = 0; i < traps.Length; ++i)
                {
                    ScriptableStructures.Trap loadedInTrap = traps[i] as ScriptableStructures.Trap;
                    Debug.Log(loadedInTrap);
                    loadoutSlots[i].SetItem(loadedInTrap);
                    this.traps[i] = loadedInTrap;
                }
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

            if (Input.GetAxis(inputConfig.LeftClickAxis) != 0)
            {
                shooter.SpawnBullet();
            }
            if (Input.GetAxis(inputConfig.RightClickAxis) != 0)
            {
                TryPlaceTrap();
            }
        }

        void TryPlaceTrap()
        {
            ScriptableStructures.Trap selected = null;
            /*
            for (int i = 0; i < loadoutSlots.Length && i < traps.Length; ++i)
            {
                if (loadoutSlots[i].Selected)
                {
                    selected = traps[i];
                    break;
                }
            }

            if (selected == null)
            {
                Debug.Log("Nothing has been selected");
                return;
            }
            if (pointReader.Data.Value < selected.Cost)
            {
                // Should be thrown as erro then caught by hud instead
                GetComponent<DefenderHUD>().SetErrorText("Not enough points");
            }
            else
            {
                PlaceTrap(selected);
            }*/
        }

        void PlaceTrap(ScriptableStructures.Trap trap)
        {
            SpawnRequestSystem spawnRequestSystem = GetComponent<LinkedEntityComponent>().World.GetExistingSystem<SpawnRequestSystem>();
            
            // Idk if traps should be structures by their nature.
            // like the 'job' structure is kind of a stretch lol.
            // just let it do animation.
            TrapConfig trapConfig = new TrapConfig
            {
                trapId = trap.PrefabPath,
                structureType = MdgSchema.Common.Structure.StructureType.Trap,
                constructionTime = trap.SetupTime,
            };

            // How i get this position prod needs to change.
            Vector3 worldCoords = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3f trapPosition = new Vector3f(worldCoords.x, 10, worldCoords.z);
            spawnRequestSystem.RequestSpawn(new MdgSchema.Common.Spawn.SpawnRequest
            {
                Position = trapPosition,
                TypeToSpawn = MdgSchema.Common.GameEntityTypes.Structure,
            }, OnTrapSpawned, Converters.SerializeArguments(trapConfig));
        }

        public void OnTrapSpawned(EntityId entityId) 
        {
            Debug.Log($"Spawned trap with entity id = {entityId}");
        }
    }
}