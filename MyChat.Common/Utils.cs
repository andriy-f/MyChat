namespace MyChat.Common
{
    using System;
    using System.Linq;
    using System.Text;

    public static class Utils
    {
        public static byte[] SerializeUTF8String(string s)
        {
            var utf8Bytes = Encoding.UTF8.GetBytes(s);
            return BitConverter.GetBytes(utf8Bytes.Length).Union(utf8Bytes).ToArray();
        }

        public static string DeSerializeUTF8String(byte[] data)
        {
            return DeSerializeUTF8String(data, 0);
        }

        public static string DeSerializeUTF8String(byte[] data, int offset)
        {
            var serializedStrLen = BitConverter.ToInt32(data, offset);
            return Encoding.UTF8.GetString(data, 4 + offset, serializedStrLen);
        }
    }
}
