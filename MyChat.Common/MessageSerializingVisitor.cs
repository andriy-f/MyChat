namespace MyChat.Common
{
    using System;

    using MyChat.Common.Models.Messages;

    public class MessageSerializingVisitor : IMessageVisitor
    {
        public byte[] Result { get; private set; }

        public void Visit(AbstractMessage message)
        {
            this.Result = Serialize((dynamic)message);
        }

        public static byte[] Serialize(SuperServiceMessage msg)
        {
            var typeBytes = BitConverter.GetBytes((int)msg.SuperMessageType);
            var dataLen = msg.DataBuffer != null ? msg.DataBuffer.Length : 0;
            var dataLenBytes = BitConverter.GetBytes(dataLen);
            var res = new byte[typeBytes.Length + dataLenBytes.Length + dataLen];
            Buffer.BlockCopy(typeBytes, 0, res, 0, typeBytes.Length);
            Buffer.BlockCopy(dataLenBytes, 0, res, typeBytes.Length, dataLenBytes.Length);
            if (dataLen > 0)
            {
                Buffer.BlockCopy(msg.DataBuffer, 0, res, typeBytes.Length + dataLenBytes.Length, dataLen);
            }

            return res;
        }

        public static byte[] Serialize(Response msg)
        {
            var idBytes = msg.Id.ToByteArray(); // 16 elements
            var dataLen = msg.Data != null ? msg.Data.Length : 0;
            var dataLenBytes = BitConverter.GetBytes(dataLen); // 4 bytes
            var res = new byte[16 + 1 + 4 + dataLen]; // GUID + bool + data length (4) + data

            Buffer.BlockCopy(idBytes, 0, res, 0, 16);
            res[16] = msg.IsSuccess ? (byte)1 : (byte)0;
            Buffer.BlockCopy(dataLenBytes, 0, res, 17, 4);
            if (dataLen > 0)
            {
                Buffer.BlockCopy(msg.Data, 0, res, 21, dataLen);
            }

            return res;
        }
    }
}