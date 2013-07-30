namespace MyChatServer.Data
{
    using MyChatServer.ChatServerDataSetTableAdapters;

    class MSSQLDataGetter : DataGetter
    {
        private LoginsTableAdapter _loginsTableAdapter;

        private MSSQLDataGetter(string connectionString)
        {
            _loginsTableAdapter = new LoginsTableAdapter();
            _loginsTableAdapter.Connection.ConnectionString = connectionString;
        }

        private MSSQLDataGetter(LoginsTableAdapter loginsTableAdapter)
        {
            _loginsTableAdapter = loginsTableAdapter;
        }

        public override bool ValidateLogin(string login)
        {
            return (int)_loginsTableAdapter.isLoginInBase(login) > 0;
        }

        public override bool ValidateLoginPass(string login, string password)
        {
            return (int)_loginsTableAdapter.ValidateLogPass(login, password) > 0;
        }


        public override void AddNewLoginPass(string login, string password)
        {
            _loginsTableAdapter.addNewLoginPass(login, password);
        }
    }
}
