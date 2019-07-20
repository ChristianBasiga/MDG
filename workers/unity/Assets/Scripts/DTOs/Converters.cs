using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MDG.DTO
{
    public class Converters
    {
        public static T DeserializeArguments<T>(byte[] serializedArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.Write(serializedArguments, 0, serializedArguments.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }


        //So my serialization is fucking up.
        public static byte[] SerializeArguments<T>(T arguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, arguments);
                return memoryStream.ToArray();
            }
        }
    }


}