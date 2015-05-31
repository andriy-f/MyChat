﻿namespace Andriy.Security.Cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class AESCSPImpl
    {
        #region Fields

        private readonly AesCryptoServiceProvider _aes;

        #endregion

        #region Constructors, Destructors

        public AESCSPImpl()
        {
            this._aes = new AesCryptoServiceProvider();
        }

        public AESCSPImpl(byte[] newKey, byte[] newIV)
        {
            if (newKey == null || newKey.Length <= 0)
                throw new ArgumentNullException("newKey");
            if (newIV == null || newIV.Length <= 0)
                throw new ArgumentNullException("newIV");

            this._aes = new AesCryptoServiceProvider {Key = newKey, IV = newIV};
        }

        public void Clear()
        {
            if (this._aes != null)
                this._aes.Clear();
        }

        #endregion

        #region end/dec bytes2bytes

        public byte[] Encrypt(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length <= 0)
                throw new ArgumentNullException("byteArray");

            byte[] res = null;

            using (MemoryStream msEncrypt = new MemoryStream()) //--> Encrypted data
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, this._aes.CreateEncryptor(),
                                                                 CryptoStreamMode.Write)) //<-- plain data
                {
                    csEncrypt.Write(byteArray, 0, byteArray.Length);
                }

                res = msEncrypt.ToArray();
            }
            return res;
        }

        public byte[] Decrypt(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");

            byte[] res = null;

            using (MemoryStream msDecrypt = new MemoryStream()) //--> Decrypted data
            {
                using (CryptoStream cs = new CryptoStream(msDecrypt, this._aes.CreateDecryptor(),
                                                          CryptoStreamMode.Write)) //<-- Encrypted data
                {
                    cs.Write(cipherText, 0, cipherText.Length);
                }
                res = msDecrypt.ToArray();
            }

            return res;
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
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, this._aes.CreateEncryptor(),
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
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, this._aes.CreateDecryptor(),
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

        ////public void Encrypt(byte[] byteArray, Stream sEncrypted)
        ////{
        ////    if (byteArray == null || byteArray.Length <= 0)
        ////        throw new ArgumentNullException("byteArray");

        ////    using (CryptoStream csEncrypt = new CryptoStream(sEncrypted, _aes.CreateEncryptor(),
        ////                                                     CryptoStreamMode.Write))
        ////        //<-- plain data
        ////    {
        ////        csEncrypt.Write(byteArray, 0, byteArray.Length);
        ////        //csEncrypt.FlushFinalBlock(); //No need as csEncrypt is closed after using
        ////    }

        ////}

        public byte[] Decrypt(Stream sCrypted)
        {
            if (sCrypted == null || !sCrypted.CanRead)
                throw new ArgumentNullException("sCrypted");

            byte[] res = null;

            using (CryptoStream csDecrypt = new CryptoStream(sCrypted, this._aes.CreateDecryptor(),
                                                             CryptoStreamMode.Read))
            {
                long len = csDecrypt.Length;
                res = new byte[len];
                csDecrypt.Read(res, 0, res.Length);
            }

            return res;
        }

        ////public byte[] Decrypt(Stream sCrypted, int length)//length of crypted or length of decrypted?
        ////{
        ////    if (sCrypted == null || !sCrypted.CanRead)
        ////        throw new ArgumentNullException("sCrypted");

        ////    byte[] res = null;

        ////    using (CryptoStream csDecrypt = new CryptoStream(sCrypted, _aes.CreateEncryptor(),
        ////                                                     CryptoStreamMode.Read))
        ////    {
        ////        res = new byte[length];
        ////        csDecrypt.Read(res, 0, length);
        ////    }

        ////    return res;
        ////}

        #endregion //Enc to Stream, dec from Stream
    }
}