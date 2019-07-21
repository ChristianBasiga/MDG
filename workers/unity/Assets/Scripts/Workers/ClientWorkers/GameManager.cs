using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Should rename schema before this gets too big.
using Mdg.Player.Metadata;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;

namespace MDG.ClientSide
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private UserInterface.UIManager uiManager;


        // Start is called before the first frame update
        void Start()
        {
            uiManager.OnRoleSelected += CreatePlayer;

        }

        // Update is called once per frame
        void Update()
        {

        }


        void CreatePlayer(PlayerType type)
        {
            UnityClientConnector connector = GetComponent<UnityClientConnector>();

            if (connector)
            {
                Debug.Log("Here???" + type.ToString());
                var playerCreationSystem = connector.Worker.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>();
                playerCreationSystem.RequestPlayerCreation(serializedArguments: DTO.Converters.SerializeArguments(new DTO.PlayerConfig
                {
                    playerType = type
                }));
            }
            else
            {
                Debug.Log("COnnector null");
            }
            
        }
    }
}