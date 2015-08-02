namespace MyChat.Common.Network
{
    using System;
    using System.Security.Cryptography;

    public class CryptoProtocol : IStreamWrapper
    {
        private readonly IStreamWrapper under;

        private readonly ICryptoTransform encryptor;

        private readonly ICryptoTransform decryptor;


        public CryptoProtocol(IStreamWrapper under, byte[] key, byte[] iv)
        {
            if (under == null)
            {
                throw new ArgumentNullException(nameof(under));
            }

            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            this.under = under;
            var aes = new AesManaged { Key = key, IV = iv };
            this.encryptor = aes.CreateEncryptor();
            this.decryptor = aes.CreateDecryptor();
        }

        public void Send(byte[] data)
        {
            var encData = this.encryptor.TransformFinalBlock(data, 0, data.Length);
            this.under.Send(encData);
        }

        public byte[] Receive()
        {
            var encData = this.under.Receive();
            var decData = this.decryptor.TransformFinalBlock(encData, 0, encData.Length);
            return decData;
        }
    }
}