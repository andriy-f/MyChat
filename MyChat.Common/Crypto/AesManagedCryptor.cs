namespace MyChat.Common.Crypto
{
    using System;
    using System.Security.Cryptography;

    public class AesManagedCryptor : IDataCryptor
    {
        private readonly ICryptoTransform encryptor;

        private readonly ICryptoTransform decryptor;

        public AesManagedCryptor(byte[] newKey, byte[] newIv)
        {
            if (newKey == null || newKey.Length <= 0)
                throw new ArgumentNullException("newKey");
            if (newIv == null || newIv.Length <= 0)
                throw new ArgumentNullException("newIv");

            var aes = new AesManaged { Key = newKey, IV = newIv };
            this.encryptor = aes.CreateEncryptor();
            this.decryptor = aes.CreateDecryptor();

        }

        public byte[] Encrypt(byte[] data)
        {
            return this.encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] Decrypt(byte[] data)
        {
            return this.decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
