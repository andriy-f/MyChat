namespace MyChat.Common.Crypto
{
    public interface IDataCryptor
    {
        byte[] Encrypt(byte[] data);

        byte[] Decrypt(byte[] data);
    }
}