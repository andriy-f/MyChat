namespace MyChat.Common
{
    using System;
    using System.Collections;

    using MyChat.Common.Models;

    public class ChatRoomInfoComparer : IComparer 
    {
        public int Compare(ChatRoomInfo x, ChatRoomInfo y)
        {
            if (x == null || y == null)
            {
                return -1;
            }

            if (x.IsPasswordProtected != y.IsPasswordProtected)
            {
                return x.IsPasswordProtected.CompareTo(y.IsPasswordProtected);
            } 
            else if (x.Name != y.Name)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
            else
            {
                return 0;
            }
        }

        public int Compare(object x, object y)
        {
            return this.Compare(x as ChatRoomInfo, y as ChatRoomInfo);
        }
    }
}