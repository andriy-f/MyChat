namespace MyChat.Common.Models.Messages
{
    public class SuperServiceMessage : AbstractMessage
    {
        public SuperServiceMessageType SuperMessageType { get; set; }

        public byte[] DataBuffer { get; set; }

        public enum SuperServiceMessageType
        {
            Simple = 0,

            Request = 1,

            Response = 2
        }
    }
}