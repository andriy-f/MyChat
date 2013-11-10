namespace Andriy.MyChat.Server.DAL
{
    using System.Collections.Concurrent;

    public class PredefinedDataGetter : DataGetter
    {
        private static readonly ConcurrentDictionary<string, string> Logins = new ConcurrentDictionary<string, string>(); 

        static PredefinedDataGetter()
        {
            Logins.TryAdd("admin", "qwe`123");
            Logins.TryAdd("user1", "qwe`123");
            Logins.TryAdd("user2", "qwe`123");
        }

        public override bool ValidateLoginPass(string login, string password)
        {
            return Logins[login] == password;
        }

        public override bool ValidateLogin(string login)
        {
            return Logins.ContainsKey(login);
        }

        public override void AddNewLoginPass(string login, string password)
        {
            Logins[login] = password;
        }
    }
}
