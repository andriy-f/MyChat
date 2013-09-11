namespace MyChatServer
{
    using System.Collections.Generic;

    public class RoomParams
    {
        //public bool isProtected=false; if !protected => pass=""
        public string password = "";//optional, if protected
        public List<string> users = new List<string>(3);//Must be list of unique            
    }
}