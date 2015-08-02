namespace MyChat.Common
{
    using System;
    using System.Text;

    public static class Utils
    {
        /// <summary>
        /// Serializes string
        /// </summary>
        /// <remarks>Serializes null and empty string the same</remarks>
        /// <param name="s">String to serialize</param>
        /// <returns>UTF-8 encoded string with length prefix</returns>
        public static byte[] SerializeUTF8String(string s)
        {
            var str = s ?? string.Empty;
            var utf8EncodedBytes = Encoding.UTF8.GetBytes(str);
            var lenPrefixBytes = BitConverter.GetBytes(utf8EncodedBytes.Length);
            var ret = new byte[lenPrefixBytes.Length + utf8EncodedBytes.Length];
            Buffer.BlockCopy(lenPrefixBytes, 0, ret, 0, lenPrefixBytes.Length);
            Buffer.BlockCopy(utf8EncodedBytes, 0, ret, lenPrefixBytes.Length, utf8EncodedBytes.Length);
            return ret;
        }

        /// <summary>
        /// Deserializes string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string DeSerializeUTF8String(byte[] data)
        {
            return DeSerializeUTF8String(data, 0);
        }

        /// <summary>
        /// Deserializes string
        /// </summary>
        /// <param name="data">UTF-8 encoded string with length prefix</param>
        /// <param name="offset">offset in byte array</param>
        /// <returns></returns>
        public static string DeSerializeUTF8String(byte[] data, int offset)
        {
            var serializedStrLen = BitConverter.ToInt32(data, offset);
            return Encoding.UTF8.GetString(data, 4 + offset, serializedStrLen);
        }
    }
}
