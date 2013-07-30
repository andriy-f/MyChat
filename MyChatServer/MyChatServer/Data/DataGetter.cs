namespace MyChatServer.Data
{
    using System.Diagnostics.CodeAnalysis;

    public abstract class DataGetter
    {
        ////private DataGetter()
        ////{
        ////}

        public static DataGetter Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        public abstract bool ValidateLoginPass(string login, string password);

        public abstract bool ValidateLogin(string login);

        public abstract void AddNewLoginPass(string login, string password);

        private class Nested
        {
            internal static readonly DataGetter instance = new PredefinedDataGetter();
            
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1409:RemoveUnnecessaryCode", Justification = "Reviewed. Suppression is OK here.")]
            static Nested()
            {
            }
        }
    }
}
