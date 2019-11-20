using MDG.Common.MonoBehaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Defender.Monobehaviours
{
    public class DefenderInputCommands : MonoBehaviour
    {
        Shooter shooter;

        // Start is called before the first frame update
        void Start()
        {
            shooter = GetComponent<Shooter>();
            GetComponent<DefenderSynchronizer>().OnEndGame += () => { this.enabled = false; };
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                shooter.SpawnBullet();
            }
        }
    }
}