namespace MyChat.Common
{
    using System;
    using System.Runtime.Serialization;

    using MyChat.Common.Models.Messages;

    public class MessageDeserializingVisitor : IMessageVisitor
    {
        public byte[] DataBuffer { get; set; }

        public int BufferOffset { get; set; }

        public int DataLength { get; set; }

        public void Visit(AbstractMessage message)
        {
            this.Deserialize((dynamic)message);
        }

        public void Deserialize(SuperServiceMessage msg)
        {
            if (this.DataBuffer == null || this.DataBuffer.Length == 0)
            {
                throw new InvalidOperationException("DataBuffer is not set or empty");
            }

            if (this.DataBuffer.Length < this.BufferOffset + this.DataLength)
            {
                throw new InvalidOperationException("DataBuffer is too short for specified offset and data length");
            }

            try
            {
                var superMessageTypeInt = BitConverter.ToInt32(this.DataBuffer, this.BufferOffset);
                msg.SuperMessageType = (SuperServiceMessage.SuperServiceMessageType)superMessageTypeInt;
                var dataLen = BitConverter.ToInt32(this.DataBuffer, this.BufferOffset + 4);
                if (dataLen > 0)
                {
                    msg.DataBuffer = new byte[dataLen];
                    Buffer.BlockCopy(this.DataBuffer, this.BufferOffset + 8, msg.DataBuffer, 0, dataLen);
                }
                else
                {
                    msg.DataBuffer = null;
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Exception during deserializing SuperServiceMessage", ex);
            }
        }

        public void Deserialize(Response msg)
        {
            if (this.DataBuffer == null || this.DataBuffer.Length == 0)
            {
                throw new InvalidOperationException("DataBuffer is not set or empty");
            }

            if (this.DataBuffer.Length < this.BufferOffset + this.DataLength)
            {
                throw new InvalidOperationException("DataBuffer is too short for specified offset and data length");
            }

            try
            {
                var guidBytes = new byte[16];
                Buffer.BlockCopy(this.DataBuffer, this.BufferOffset, guidBytes, 0, 16);
                msg.Id = new Guid(guidBytes);

                msg.IsSuccess = this.DataBuffer[this.BufferOffset + 16] == 1;

                var dataLen = BitConverter.ToInt32(this.DataBuffer, this.BufferOffset + 17);
                if (dataLen > 0)
                {
                    msg.Data = new byte[dataLen];
                    Buffer.BlockCopy(this.DataBuffer, this.BufferOffset + 21, msg.Data, 0, dataLen);
                }
                else
                {
                    msg.Data = null;
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Exception during deserializing Response", ex);
            }
        }
    }
}