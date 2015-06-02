namespace MyChat.Common.Network
{
    public interface IStreamWrapper
    {
        void Send(byte[] data);

        byte[] Receive();
    }
}