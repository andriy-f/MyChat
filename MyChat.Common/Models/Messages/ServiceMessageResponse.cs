namespace MyChat.Common.Models.Messages
{
    using System;

    public class ServiceMessageResponse
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public static ServiceMessageResponse FromBytes(byte[] input)
        {
            var res = new ServiceMessageResponse();
            res.IsSuccess = BitConverter.ToBoolean(input, 0);
            res.Message = Utils.DeSerializeUTF8String(input, 1);
            return res;
        }

        public byte[] ToBytes()
        {
            var serializedMessage = Utils.SerializeUTF8String(this.Message);
            var res = new byte[1 + serializedMessage.Length];
            res[0] = BitConverter.GetBytes(this.IsSuccess)[0];
            serializedMessage.CopyTo(res, 1);
            return res;
        }
    }
}
