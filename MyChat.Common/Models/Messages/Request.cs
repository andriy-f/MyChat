namespace MyChat.Common.Models.Messages
{
    using System;

    [Serializable]
    public class Request : AbstractMessage
    {
        public Guid Id { get; set; }

        public RequestTypeEnum RequestType { get; set; }

        public byte[] Data { get; set; }
    }
}