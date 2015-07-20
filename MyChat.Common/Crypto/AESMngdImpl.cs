namespace MyChat.Common.Crypto
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class AESMngdImpl : IDataCryptor, IDisposable
    {
        private AesManaged _aes;

        private ICryptoTransform _encryptor;

        private ICryptoTransform _decryptor;

        #region Constructors, Destructors

        public AESMngdImpl()
        {
            this._aes = new AesManaged();
            this._encryptor = this._aes.CreateEncryptor();
            this._decryptor = this._aes.CreateDecryptor();
        }

        public AESMngdImpl(byte[] newKey, byte[] newIV)
        {
            if (newKey == null || newKey.Length <= 0)
            {
                throw new ArgumentNullException("newKey");
            }

            if (newIV == null || newIV.Length <= 0)
            {
                throw new ArgumentNullException("newIV");
            }

            this._aes = new AesManaged { Key = newKey, IV = newIV };
            this._encryptor = this._aes.CreateEncryptor();
            this._decryptor = this._aes.CreateDecryptor();
        }

        public void Clear()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool freeManagedObjects)
        {
            if (freeManagedObjects)
            {
                if (this._encryptor != null)
                {
                    this._encryptor.Dispose();
                    this._encryptor = null;
                }

                if (this._decryptor != null)
                {
                    this._decryptor.Dispose();
                    this._decryptor = null;
                }

                if (this._aes != null)
                {
                    this._aes.Dispose();
                    this._aes = null;
                }
            }
        }

        #endregion

        #region end/dec bytes2bytes

        public byte[] Encrypt(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length <= 0)
            {
                throw new ArgumentNullException("byteArray");
            }

            return this._encryptor.TransformFinalBlock(byteArray, 0, byteArray.Length);
        }

        public byte[] Decrypt(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }

            return this._decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }

        #endregion

        #region enc str2bytes, dec bytes2str

        public byte[] EncryptStr(string plainText)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");

            byte[] res = null;
            using (MemoryStream msEncrypt = new MemoryStream()) //--> encrypted data
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, this._encryptor,
                                                                 CryptoStreamMode.Write)) //<--plain data
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) //<-- plain data(string)  
                    {
                        swEncrypt.Write(plainText);
                    }
                }
                res = msEncrypt.ToArray();
            }
            return res;
        }

        public string DecryptStr(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");

            string res = null;
            using (MemoryStream msDecrypt = new MemoryStream(cipherText)) //<-- encrypted data
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, this._decryptor,
                                                                 CryptoStreamMode.Read)) //--> decrypted data
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt)) //--> decrypted data (string)
                    {
                        res = srDecrypt.ReadToEnd();
                    }
                }
            }
            return res;
        }

        #endregion //enc str2bytes, dec bytes2str

        #region Enc to Stream, dec from Stream

        public void Encrypt(byte[] byteArray, Stream sEncrypted)
        {
            if (byteArray == null || byteArray.Length <= 0)
                throw new ArgumentNullException("byteArray");

            using (CryptoStream csEncrypt = new CryptoStream(sEncrypted, this._encryptor,
                                                             CryptoStreamMode.Write))
            //<-- plain data
            {
                csEncrypt.Write(byteArray, 0, byteArray.Length);
                //csEncrypt.FlushFinalBlock(); //No need as csEncrypt is closed after using
            }

        }

        public byte[] Decrypt(Stream sCrypted)
        {
            if (sCrypted == null || !sCrypted.CanRead)
                throw new ArgumentNullException("sCrypted");

            byte[] res = null;

            using (CryptoStream csDecrypt = new CryptoStream(sCrypted, this._decryptor,
                                                             CryptoStreamMode.Read))
            {
                long len = csDecrypt.Length;
                res = new byte[len];
                csDecrypt.Read(res, 0, res.Length);
            }

            return res;
        }

        public byte[] Decrypt(Stream sCrypted, int length)//length of crypted or length of decrypted?
        {
            if (sCrypted == null || !sCrypted.CanRead)
                throw new ArgumentNullException("sCrypted");

            byte[] res = null;

            using (CryptoStream csDecrypt = new CryptoStream(sCrypted, this._decryptor,
                                                             CryptoStreamMode.Read))
            {
                res = new byte[length];
                csDecrypt.Read(res, 0, length);
            }

            return res;
        }

        #endregion //Enc to Stream, dec from Stream
    }
}
