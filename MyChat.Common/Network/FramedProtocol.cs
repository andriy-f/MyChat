namespace MyChat.Common.Network
{
    using System;
    using System.IO;

    public class FramedProtocol : IStreamWrapper
    {
        private readonly Stream wrappedStream;

        public FramedProtocol(Stream stream)
        {
            this.wrappedStream = stream;
        }

        public void Send(byte[] bytes)
        {
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            this.wrappedStream.Write(data, 0, data.Length);
        }

        public byte[] Receive()
        {
            int streamDataSize = ReadInt32(this.wrappedStream);
            var streamData = new byte[streamDataSize];
            this.wrappedStream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        private static int ReadInt32(Stream stream)
        {
            var data = new byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }
    }
}
