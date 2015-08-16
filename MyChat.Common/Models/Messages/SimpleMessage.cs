namespace MyChat.Common.Models.Messages
{
    public class SimpleMessage
    {
        public SimpleMessageTypeEnum MessageType { get; set; }

        public byte[] Data { get; set; }

        public enum SimpleMessageTypeEnum
        {
            TextMessage
        }
    }
}