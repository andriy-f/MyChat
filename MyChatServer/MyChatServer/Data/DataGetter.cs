using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyChatServer.Data
{
    abstract class DataGetter
    {
        protected DataGetter()
        {
        }

        //public abstract bool ValidateLoginHash(string login, string hash);

        public abstract bool ValidateLoginPass(string login, string password);

        public abstract bool ValidateLogin(string login);

        public abstract void AddNewLoginPass(string login, string password);
    }
}
