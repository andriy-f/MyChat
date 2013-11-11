namespace Andriy.MyChat.Server.DAL
{
    public interface IDataContext
    {
        bool ValidateLoginPass(string login, string password);

        bool LoginExists(string login);

        void AddUser(string login, string password); 
    }
}