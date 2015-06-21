namespace MyChat.Common.Models.Messages
{
    using System.Linq;

    public class ServiceMessage
    {
        /// <summary>
        /// 1 byte
        /// </summary>
        public MessageType MessageType { get; set; }

        public byte[] Data { get; set; }

        public static ServiceMessage FromBytes(byte[] input)
        {
            var res = new ServiceMessage();
            res.MessageType = (MessageType)input[0];
            res.Data = input.Skip(1).ToArray();
            return res;
        }

        public byte[] ToBytes()
        {
            var res = new byte[1 + this.Data.Length];
            res[0] = (byte)this.MessageType;
            this.Data.CopyTo(res, 1);
            return res;
        }
    }
}
