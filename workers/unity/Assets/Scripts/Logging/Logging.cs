using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using log4net;

namespace MDG.Logging
{
    public class LoggingManager : MonoBehaviour
    {

        private static readonly ILog Log = LogManager.GetLogger(typeof(MonoBehaviour));
        // Start is called before the first frame update
        
        public ILog GetMonoLogger
        {
            get { return Log; }
        }

       
    }
}