using System.IO;
using UnityEngine;
//using log4net.Config;

namespace MDG.Logging
{
    public static class Configuration 
    {
      
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            FileInfo fileInfo = new FileInfo($"{Application.dataPath}/Config/log4net.xml");
          //  XmlConfigurator.Configure(fileInfo);
        }
    }
}