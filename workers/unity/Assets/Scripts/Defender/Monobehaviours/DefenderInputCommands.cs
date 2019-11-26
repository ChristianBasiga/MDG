using MDG.Common.MonoBehaviours;
using MDG.ScriptableObjects.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Defender.Monobehaviours
{
    public class DefenderInputCommands : MonoBehaviour
    {
        Shooter shooter;
        InputConfig inputConfig;
        // Start is called before the first frame update
        void Start()
        {
            shooter = GetComponent<Shooter>();
            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };
        }

        public void Init(InputConfig inputConfig)
        {
            this.inputConfig = inputConfig;
        }


        // Update is called once per frame
        void Update()
        {



            if (Input.GetAxis(inputConfig.LeftClickAxis) != 0)
            {
                shooter.SpawnBullet();
            }
        }
    }
}