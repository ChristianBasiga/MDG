using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Player;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using MDG.Invader.Systems;
using MDG.Common.Systems;

using Improbable.Worker.CInterop.Query;
using Improbable.Gdk.Core.Commands;
using Unity.Collections;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Zenject;
using SpawnSchema = MdgSchema.Common.Spawn;

using MdgSchema.Common;

namespace MDG.ClientSide
{
    public class GameManager : MonoBehaviour
    {


        int levelWidth;
        int levelLength;
        public Mesh mesh;
        public Material material;
        CommandSystem commandSystem;

        [SerializeField]
        private UserInterface.UIManager uiManager;
        GameEntityTypes playerSelected;
        
        public void Init(int levelLength, int levelWidth)
        {
            this.levelWidth = levelWidth;
            this.levelLength = levelLength;
        }
        
        // Start is called before the first frame update

        void Start()
        {
            uiManager.OnRoleSelected += CreatePlayer;
        }

        // Ideally launcher opens this and select player already, but for nw this is fine.
        void CreatePlayer(GameEntityTypes type)
        {
            UnityClientConnector connector = GetComponent<UnityClientConnector>();
            playerSelected = type;
            if (connector)
            {
                try
                {
                    /*
                    CommandSystem commandSystem = connector.Worker.World.GetOrCreateSystem<CommandSystem>();

                    commandSystem.SendCommand(new SpawnSchema.SpawnManager.SpawnGameEntity.Request
                    {

                        TargetEntityId = new EntityId(2),
                        
                        Payload = new SpawnSchema.SpawnRequest
                        {
                            TypeToSpawn = MdgSchema.Common.GameEntityTypes.Hunter,
                        }
                    });

                    */
                    var playerCreationSystem = connector.Worker.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();
                    playerCreationSystem.RequestPlayerCreation(serializedArguments: DTO.Converters.SerializeArguments(new DTO.PlayerConfig
                    {
                        playerType = type,
                    }), OnCreatePlayerResponse);
                }
                catch(System.Exception err)
                {
                }
            }
            else
            {
                Debug.Log("COnnector null");
            }
            
        }

        //Move this and the creation requests to manager and just have this call it from manager.
        private void OnCreatePlayerResponse(PlayerCreator.CreatePlayer.ReceivedResponse response)
        {
            if (response.StatusCode != Improbable.Worker.CInterop.StatusCode.Success)
            {
                Debug.LogWarning($"Error: {response.Message}");
            }
            else
            {
              
            }
        }

        private void Update()
        {
           
        }
    }
}