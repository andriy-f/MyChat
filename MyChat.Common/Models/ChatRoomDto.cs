namespace MyChat.Common.Models
{
    using System;

    [Serializable]
    public class ChatRoomInfo
    {
        public string Name { get; set; }

        public bool IsPasswordProtected { get; set; }
    }
}