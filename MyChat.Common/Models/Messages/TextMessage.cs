namespace MyChat.Common.Models.Messages
{
    using System;

    [Serializable]
    public class TextMessage
    {
        public string Source { get; set; }

        public string Destination { get; set; }

        public string Text { get; set; }

        public DestinationTypeEnum DestinationType { get; set; }

        public enum DestinationTypeEnum
        {
            User,
            Room,
            All
        }
    }
}