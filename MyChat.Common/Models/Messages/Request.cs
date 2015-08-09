namespace MyChat.Common.Models.Messages
{
    using System;

    public class Request
    {
        public Guid Id { get; set; }

        public byte[] Data { get; set; }
    }
}