using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace MyChat
{
    class MsgProcessor
    {
        public delegate void ReceiveMsgProcessor(string source, string dest, string msg);

        class RoomParams
        {
            public ReceiveMsgProcessor processor;

            public RoomParams(ReceiveMsgProcessor prc)
            { processor = prc; }
        }

        private Hashtable roomProcessors = new Hashtable(5);//roomName/RoomParams

        public int RoomCount
        { get { return roomProcessors.Count; } }

        public bool addProcessor(string room, ReceiveMsgProcessor proc)//When user joins room
        {
            if (!roomProcessors.Contains(room))
            {
                roomProcessors.Add(room, new RoomParams(proc));
                return true;
            }
            else return false;
        }

        public bool removeProcessor(string room)//When user leaves room
        {
            if (roomProcessors.Contains(room))
            {
                roomProcessors.Remove(room);
                return true;
            }
            else return false;
        }

        public void process(string source, string dest, string message)//For user or All
        {
            foreach (System.Collections.DictionaryEntry de in roomProcessors)
                ((RoomParams)de.Value).processor(source, dest, message);
        }

        public bool processForRoom(string source, string dest, string message)//room==dest
        {
            if (roomProcessors.Contains(dest))
            {
                ((RoomParams)roomProcessors[dest]).processor(source, dest, message);
                return true;
            }
            else return false;
        }
    } 
}
