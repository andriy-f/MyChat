namespace Andriy.MyChat.Server
{
    public class Credentials
    {
        public string Login { get; set; }

        public string Pasword { get; set; }

        public static Credentials Parse(byte[] bytes, int offset = 1)
        {
            // Read header = type+loginSize+passSize (9 bytes)
            const int HeaderSize = 9;

            // bytes[0] must be 0
            int loginSize = System.BitConverter.ToInt32(bytes, offset);
            int passSize = System.BitConverter.ToInt32(bytes, offset + 4);

            // Read data
            var login = System.Text.Encoding.UTF8.GetString(bytes, HeaderSize, loginSize);
            var pass = System.Text.Encoding.UTF8.GetString(bytes, HeaderSize + loginSize, passSize);

            return new Credentials { Login = login, Pasword = pass };
        }
    }
}