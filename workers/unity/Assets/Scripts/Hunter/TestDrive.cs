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
using MdgSchema.Units;
using MdgSchema.Spawners;
using Zenject;

namespace MDG.Hunter.Testing
{
    //This will essentially be the monobehaviour of the hunter.
    public class TestDrive : MonoBehaviour
    {
        [SerializeField]
        public Dictionary<Commands.CommandType, TextAsset[]> commandToBehaviourTreesScripts;
        private CustomGameObjectCreator CustomGameObjectCreator;
        public struct PendingCommand
        {
            public EntityId commandListener;
            public System.Type type;
            public CommandListener commandPayload;
        }
        Queue<PendingCommand> pendingCommands;

        void Initialize(CustomGameObjectCreator customGameObjectCreator)
        {

        }
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

            CommandUpdateSystem.OnCommandExecute += QueueCommandToExecute;
            StartCoroutine(ExecutePendingCommandRoutine());
        }


        void QueueCommandToExecute(EntityId commandListener, System.Type type, CommandListener commandPayload)
        {
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