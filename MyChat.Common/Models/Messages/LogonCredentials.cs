namespace MyChat.Common.Models.Messages
{
    using System.IO;

    public class LogonCredentials
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public static LogonCredentials ReadFromStream(Stream source)
        {
            var reader = new BinaryReader(source);
            var res = new LogonCredentials();
            res.Login = reader.ReadString();
            res.Password = reader.ReadString();
            return res;
        }

        public static LogonCredentials FromBytes(byte[] data)
        {
            // TODO: using
            var stream = new MemoryStream(data);
            return ReadFromStream(stream);
        }
        
        public void WriteToStream(Stream output)
        {
            var writer = new BinaryWriter(output);
            writer.Write(this.Login);
            writer.Write(this.Password);
        }

        public byte[] ToBytes()
        {
            // TODO: using
            var stream = new MemoryStream();
            this.WriteToStream(stream);
            return stream.ToArray();
        }
    }
}
