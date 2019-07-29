using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using log4net;

namespace MDG.Logging
{
    //Test script.
    public class Logging : MonoBehaviour
    {

        private static readonly ILog Log = LogManager.GetLogger(typeof(MonoBehaviour));
        // Start is called before the first frame update
        void Start()
        {
               
        }

        // Update is called once per frame
        void Update()
        {

            if (Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log(Log.IsDebugEnabled);
                Log.Debug("hello");

            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log(Log.IsWarnEnabled);
                Log.Warn("world");
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log(Log.IsErrorEnabled);
                Log.Error("error");
            }
        }
    }
}