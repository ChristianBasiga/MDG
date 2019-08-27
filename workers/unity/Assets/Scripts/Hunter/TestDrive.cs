using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common;
using MDG.Common.Systems;
using MDG.Hunter.Components;
using MDG.Hunter.Monobehaviours;
using MDG.Common.Components;
using MDG.Hunter.Systems;
using System.Linq;
using Improbable.Gdk.Subscriptions;
using MdgSchema.Common;
using MdgSchema.Spawners;

namespace MDG.Hunter.Testing
{
    //This will essentially be the monobehaviour of the hunter.
    public class TestDrive : MonoBehaviour
    {
        public Camera hunterCamera;
        public ClickableMonobehaviour resourceClickable;



     

        [SerializeField]
        public ClickableMonobehaviour[] units;
       
        [SerializeField]
        public Dictionary<Commands.CommandType, TextAsset[]> commandToBehaviourTreesScripts;

        //For reference to entity to game object mapping.
        private CustomGameObjectCreator CustomGameObjectCreator;


        public struct PendingCommand
        {
            public EntityId commandListener;
            public System.Type type;
            public CommandListener commandPayload;
        }
        Queue<PendingCommand> pendingCommands;




        // Start is called before the first frame update
        void Start()
        {
            
            commandToBehaviourTreesScripts = new Dictionary<Commands.CommandType, TextAsset[]>();
            pendingCommands = new Queue<PendingCommand>();


            string[] commandTypes = System.Enum.GetNames(typeof(Commands.CommandType));
            foreach (string commandType in commandTypes)
            {
                commandToBehaviourTreesScripts[(Commands.CommandType)System.Enum.Parse(typeof(Commands.CommandType), commandType)] =
                    Resources.LoadAll<TextAsset>($"BehaviourTreeScripts/{commandType}/");
            }

            this.CustomGameObjectCreator = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().customGameObjectCreator;
            // Another problem with this is that since all same world and all share same system.
            // then when another command giver causes event to invoke, this client will
            // will invoke on their version of the component as they share the same id.
            // which also brings to question of one entity mapped to multiple gameobjects.
            // may not be problem for case of move commands, since essentially will cause them to be in sync
            // but will be problem for collect  / attack commands. So how I queue up commands to run on a Unit has to change as well
            // unless I only process if in my scene it is authoritative
            // that's a crucial detail. So it will be entity to GameObjects mapping.
            // more often than not there will only be two gameObjects per entry.
            // I check which is in my scene, and that is how I'll know.
            // still requires a find, but can think of few ways to avoid that, but for now its fine.
            CommandUpdateSystem.OnCommandExecute += QueueCommandToExecute;
            StartCoroutine(ExecutePendingCommandRoutine());
        }

        // Could have button, then on click, it needs to send update to UnitSpawner.
        // So could create UnitSpawner as a SpatialOS entity to only spawn in client side.
        // I have entityId of myself as hunter. With that I can update component.
        // so instead of creating new entity as unit spawner can add unit spawner to Hunter.
        // makes updates to it more seemless, which would be ideal and makes sense, I mean hunter is a unit spawner.
        public void CreateUnit(UnitTypes typeToCreate)
        {

            //In future unit spawner should also take in kind of Unit to spawn.
            //Maybe client connector / game manager have this.
            //Game manager should also be an entity honestly.
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            //Can turn UnitSpawner to non spatial later
            Entity unitSpawner = linkedEntityComponent.Worker.EntityManager.CreateEntity(typeof(UnitSpawner.Component));
            //Get position too???????? mayhaps its own system.
           
            linkedEntityComponent.Worker.EntityManager.SetComponentData(unitSpawner, new UnitSpawner.Component {

                AmountToSpawn = 1,
                Position = new Improbable.Coordinates
                {
                    X = 1.0,
                    Y = 10.0,
                    Z = 1.0
                } });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                CreateUnit(UnitTypes.COLLECTOR);
            }
        }

        void QueueCommandToExecute(EntityId commandListener, System.Type type, CommandListener commandPayload)
        {

            // To add before queing command to execute
            // verify that I have authority over this entity.
            PendingCommand pendingCommand = new PendingCommand
            {
                commandListener = commandListener,
                type = type,
                commandPayload = commandPayload
            };
            pendingCommands.Enqueue(pendingCommand);
        }


        private IEnumerator ExecutePendingCommandRoutine()
        {
            while (enabled)
            {
                while (pendingCommands.Count > 0)
                {
                    PendingCommand pendingCommand = pendingCommands.Dequeue();
                    //For now fetch all units.
                    List<GameObject> gameObjects = CustomGameObjectCreator.GetLinkedGameObjectById(pendingCommand.commandListener);
                    GameObject gameObject = gameObjects[0];
                    //Select the one gameobject which is in your scene
                    //If i'm right elements will awlays be one since literally diff entities.
                    if (GameObject.Find(gameObjects[0].name))
                    {
                        gameObject = gameObjects[0];
                    }
                    else
                    {
                        gameObject = gameObjects[1];
                    }

                    UnitBehaviour currentCommand = gameObject.GetComponent<UnitBehaviour>();

                    if (currentCommand != null)
                    {
                        if (!currentCommand.GetType().Equals(pendingCommand.type))
                        {
                            Destroy(currentCommand);
                            currentCommand = gameObject.AddComponent(pendingCommand.type) as UnitBehaviour;
                        }
                    }
                    else
                    {
                        currentCommand = gameObject.AddComponent(pendingCommand.type) as UnitBehaviour;
                    }
                    currentCommand.Initialize(pendingCommand.commandListener, pendingCommand.commandPayload);
                    
                    //Composition, cause should always have behaviour.
                    //Panda.BehaviourTree behaviourTree = gameObject.GetComponent<Panda.BehaviourTree>();
                    // behaviourTree.scripts = commandToBehaviourTreesScripts[pendingCommand.commandPayload.CommandType];
                    //behaviourTree.Reset();
                }
                yield return new WaitUntil(() => { return pendingCommands.Count > 0; });
            }
        }
       
    }
}