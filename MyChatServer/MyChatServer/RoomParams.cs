namespace MyChatServer
{
    using System.Collections.Generic;

    public class RoomParams
    {
        ////public bool isProtected=false; if !protected => pass=""
        
        private string password = string.Empty; // Optional, if protected
        
        private readonly List<string> users = new List<string>(3); // Must be list of unique 
        
        public string Password
        {
            get
            {
                return this.password;
            }

            set
            {
                this.password = value;
            }
        }
        
        public List<string> Users
        {
            get
            {
                return this.users;
            }
        }
    }
}