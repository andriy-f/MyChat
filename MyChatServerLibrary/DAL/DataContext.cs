namespace Andriy.MyChat.Server.DAL
{
    using System.Diagnostics.CodeAnalysis;

    public abstract class DataContext : IDataContext
    {
        // ReSharper disable EmptyConstructor
        protected DataContext()
            // ReSharper restore EmptyConstructor
        {
        }

        public static IDataContext Instance
        {
            get
            {
                return Nested.DataInstance;
            }
        }

        public abstract bool ValidateLoginPass(string login, string password);

        public abstract bool LoginExists(string login);

        public abstract void AddUser(string login, string password);

        private class Nested
        {
            internal static readonly IDataContext DataInstance = new MemoryDataContext();
            
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1409:RemoveUnnecessaryCode", Justification = "Reviewed. Suppression is OK here.")]
            static Nested()
            {
            }
        }
    }
}
