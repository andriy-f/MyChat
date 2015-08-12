namespace MyChat.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using MyChat.Common.Models;

    public class CustomBinaryFormatter
    {
        public static byte[] Serialize(IEnumerable<ChatRoomInfo> set)
        {
            var formatter = new BinaryFormatter();
            byte[] content;
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, set);
                ms.Position = 0;
                content = ms.GetBuffer();
            }

            return content;
        }

        public static IEnumerable<ChatRoomInfo> Deserialize(byte[] data, int start, int count)
        {
            using (var ms = new MemoryStream(data, start, count))
            {
                ////ms.Seek(0, SeekOrigin.Begin);
                var bf = new BinaryFormatter();
                var res = bf.Deserialize(ms);
                return (IEnumerable<ChatRoomInfo>)res;
            }
        }
    }
}