using System.IO;
using UnityEngine;
using log4net.Config;

namespace MDG.Logging
{
    public static class Configuration 
    {
      
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Debug.Log("I am called");
            FileInfo fileInfo = new FileInfo($"{Application.dataPath}/Config/log4net.xml");
            Debug.Log(fileInfo.FullName);
            XmlConfigurator.Configure(fileInfo);
        }
    }
}