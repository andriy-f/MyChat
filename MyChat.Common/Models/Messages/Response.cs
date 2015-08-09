namespace MyChat.Common.Models.Messages
{
    using System;

    public class Response
    {
        public Guid Id { get; set; }

        public bool IsSuccess { get; set; }

        public byte[] Data { get; set; }        
    }
}