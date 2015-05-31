namespace Andriy.MyChat.Server.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BinaryFormatter
    {
        // TODO: use
        ////public byte[] ToBytes(string[] strings)
        ////{
        ////    int i, n = strings.Length;
        ////    var stringBytes = new byte[n][];
        ////    int msgLen = 1 + 4; // type identifier + strings count
        ////    for (i = 0; i < n; i++)
        ////    {
        ////        stringBytes[i] = Encoding.UTF8.GetBytes(strings[i]);
        ////        msgLen += 4 + stringBytes[i].Length; // string byte representation length
        ////    }

        ////    // Formatting Message
        ////    var data = new byte[msgLen];
        ////    data[0] = 0; // type
        ////    var roomCntB = BitConverter.GetBytes(n);
        ////    roomCntB.CopyTo(data, 1);
        ////    int pos = 5;
        ////    for (i = 0; i < n; i++)
        ////    {
        ////        var stringLengthBytes = BitConverter.GetBytes(stringBytes[i].Length); // 4 bytes
        ////        stringLengthBytes.CopyTo(data, pos);
        ////        pos += 4;
        ////        stringBytes[i].CopyTo(data, pos);
        ////        pos += stringBytes[i].Length;
        ////    }

        ////    return data;
        ////}
    }
}
