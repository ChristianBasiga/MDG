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

        ScriptableStructures.Trap[] traps;
        // Start is called before the first frame update
        void Start()
        {
            shooter = GetComponent<Shooter>();
            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };

            // FOr now just load in all traps and set it.
            traps = Resources.LoadAll("ScriptableObjects/Traps") as ScriptableStructures.Trap[];
            for (int i = 0; i < traps.Length; ++i)
            {
                loadoutSlots[i].SetItem(traps[i]);
            }
        }

        public void Init(InputConfig inputConfig)
        {
            this.inputConfig = inputConfig;
        }

        // Update is called once per frame
        void Update()
        {

            for (int i = 0; i <= loadoutSlots.Length; ++i)
            {
                loadoutSlots[i].Toggle(Input.GetKeyDown((i + 1).ToString()));
            }


            if (Input.GetAxis(inputConfig.LeftClickAxis) != 0)
            {
                shooter.SpawnBullet();
            }
            else if (Input.GetAxis(inputConfig.RightClickAxis) != 0)
            {
                TryPlaceTrap();
            }
        }

        void TryPlaceTrap()
        {
            ScriptableStructures.Trap selected = traps[0];
            for (int i = 0; i < loadoutSlots.Length; ++i)
            {
                if (loadoutSlots[i].Selected)
                {
                    selected = traps[i];
                    break;
                }
            }

            if (pointReader.Data.Value < selected.Cost)
            {
                // Should be thrown as erro then caught by hud instead
                GetComponent<DefenderHUD>().SetErrorText("Not enough points");
            }
            else
            {
                PlaceTrap(selected);
            }
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