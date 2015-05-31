namespace Andriy.MyChat.Client
{
    using System.Collections;

    public class MsgProcessor
    {
        public delegate void ReceiveMsgProcessor(string source, string dest, string msg);

        class RoomParams
        {
            public ReceiveMsgProcessor processor;

            public RoomParams(ReceiveMsgProcessor prc)
            { this.processor = prc; }
        }

        private Hashtable roomProcessors = new Hashtable(5);//roomName/RoomParams

        public int RoomCount
        { get { return this.roomProcessors.Count; } }

        public bool addProcessor(string room, ReceiveMsgProcessor proc)//When user joins room
        {
            if (!this.roomProcessors.Contains(room))
            {
                this.roomProcessors.Add(room, new RoomParams(proc));
                return true;
            }
            else return false;
        }

        public bool removeProcessor(string room)//When user leaves room
        {
            if (this.roomProcessors.Contains(room))
            {
                this.roomProcessors.Remove(room);
                return true;
            }
            else return false;
        }

        public void process(string source, string dest, string message)//For user or All
        {
            foreach (System.Collections.DictionaryEntry de in this.roomProcessors)
                ((RoomParams)de.Value).processor(source, dest, message);
        }

        public bool processForRoom(string source, string dest, string message)//room==dest
        {
            if (this.roomProcessors.Contains(dest))
            {
                ((RoomParams)this.roomProcessors[dest]).processor(source, dest, message);
                return true;
            }
            else return false;
        }
    } 
}
