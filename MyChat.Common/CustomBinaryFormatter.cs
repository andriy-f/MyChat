namespace MyChat.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using MyChat.Common.Models;
    using MyChat.Common.Models.Messages;

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

        public static byte[] Serialize(object something)
        {
            var formatter = new BinaryFormatter();
            byte[] content;
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, something);
                ms.Position = 0;
                content = ms.GetBuffer();
            }

            return content;
        }

        public static byte[] Serialize(Response response)
        {
            var formatter = new BinaryFormatter();
            byte[] content;
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, response);
                ms.Position = 0;
                content = ms.GetBuffer();
            }

            return content;
        }

        public static byte[] Serialize(SuperServiceMessage msg)
        {
            var formatter = new BinaryFormatter();
            byte[] content;
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, msg);
                ms.Position = 0;
                content = ms.GetBuffer();
            }

            return content;
        }

        public static object Deserialize(byte[] data, int start, int count)
        {
            using (var ms = new MemoryStream(data, start, count))
            {
                var bf = new BinaryFormatter();
                var res = bf.Deserialize(ms);
                return res;
            }
        }

        public static IEnumerable<ChatRoomInfo> DeserializeChatRoomInfos(byte[] data, int start, int count)
        {
            using (var ms = new MemoryStream(data, start, count))
            {
                ////ms.Seek(0, SeekOrigin.Begin);
                var bf = new BinaryFormatter();
                var res = bf.Deserialize(ms);
                return (ChatRoomInfo[])res;
            }
        }

        public static Response DeserializeResponse(byte[] data, int start, int count)
        {
            using (var ms = new MemoryStream(data, start, count))
            {
                ////ms.Seek(0, SeekOrigin.Begin);
                var bf = new BinaryFormatter();
                var res = bf.Deserialize(ms);
                return (Response)res;
            }
        }

        public static SuperServiceMessage DeserializeSuperServiceMessage(byte[] data, int start, int count)
        {
            using (var ms = new MemoryStream(data, start, count))
            {
                ////ms.Seek(0, SeekOrigin.Begin);
                var bf = new BinaryFormatter();
                var res = bf.Deserialize(ms);
                return (SuperServiceMessage)res;
            }
        }
    }
}