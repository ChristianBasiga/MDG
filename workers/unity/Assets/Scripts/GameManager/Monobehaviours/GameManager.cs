using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
using GameSchema = MdgSchema.Game;
using MdgSchema.Common;
using MDG.Common.MonoBehaviours;

namespace MDG.Game.Monobehaviours 
{
    public class GameManager : MonoBehaviour
    {

        [Require] GameSchema.GameStatusReader gameStatusReader;


        int levelWidth;
        int levelLength;
        CommandSystem commandSystem;
        ComponentUpdateSystem componentUpdateSystem;

        
        public void Init(int levelLength, int levelWidth)
        {
            this.levelWidth = levelWidth;
            this.levelLength = levelLength;
        }
        
        // Start is called before the first frame update

        void Start()
        {
            // UI manager should handle this.
            gameStatusReader.OnTimeLeftUpdate += GameObject.Find("ClientWorker").GetComponent<MainOverlayHUD>().UpdateTime;
        }

        private void Update()
        {
            // Why did this take me over an hour. Not thinking right????? wat the actual fuck.
            // not hard. Just needed to check for it like I check for events
            // cause update is just ane vent lol. God.
           /* if (componentUpdateSystem != null)
            {
                var updates = componentUpdateSystem.GetComponentUpdatesReceived<GameSchema.GameStatus.Update>();
                if (updates.Count > 0)
                {
                    var update = updates[0];
                    if (update.Update.TimeLeft.TryGetValue(out float updatedTime))
                    {
                        GameTimerUpdate(updatedTime);
                    }
                }
            }*/
        }
    }
}