using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;


namespace My.Cryptography
{
    public class AesWrapper
    {
        #region Fields

        private const int AesBlockLen = 16;
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;
        private readonly bool _CAPI;

        #endregion

        #region Constructors, Destructors

        public AesWrapper(bool CAPI)
        {
            _CAPI = CAPI;
            if (CAPI) _aes = new AesCryptoServiceProvider();
            else _aes = new AesManaged();
            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        public AesWrapper(bool CAPI, byte[] newKey, byte[] newIV)
        {
            if (newKey == null || newKey.Length <= 0)
                throw new ArgumentNullException("newKey");
            if (newIV == null || newIV.Length <= 0)
                throw new ArgumentNullException("newIV");

            _CAPI = CAPI;
            if (CAPI) _aes = new AesCryptoServiceProvider();
            else _aes = new AesManaged();

            _aes.Key = newKey;
            _aes.IV = newIV;
            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        public void Clear()
        {
            if (_encryptor != null)
                _encryptor.Dispose();
            if (_decryptor != null)
                _decryptor.Dispose();

            if (_aes != null)
                _aes.Clear();
            
            //Console.WriteLine("AesWrapper cleared!");
        }

        ~AesWrapper()
        {
           Clear();
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
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, _encryptor,
                                                                 CryptoStreamMode.Write)) //<-- plain data
                {
                    csEncrypt.Write(byteArray, 0, byteArray.Length);
                }

                res = msEncrypt.ToArray();
            }
            return res;
        }

        //If _decryptor is used second time and AesCSP(CAPI) is used for decryption -> CryptographicEx: Padding is invalid and cannot be removed
        public byte[] Decrypt(byte[] cipherData)
        {
            if (cipherData == null || cipherData.Length <= 0)
                throw new ArgumentNullException("cipherData");

            byte[] res = null;

            using (MemoryStream msDecrypt = new MemoryStream()) //--> Decrypted data
            {
                ICryptoTransform decryptor = _CAPI ? _aes.CreateDecryptor() : _decryptor;
                using (CryptoStream cs = new CryptoStream(msDecrypt, decryptor,//_decryptor, 
                                                          CryptoStreamMode.Write)) //<-- Encrypted data
                {
                    cs.Write(cipherData, 0, cipherData.Length);
                }
                res = msDecrypt.ToArray();
            }

            return res;
        }

        [Obsolete]
        public byte[] Decrypt2(byte[] cipherData, out int realLength)
        {
            if (cipherData == null || cipherData.Length <= 0)
                throw new ArgumentNullException("cipherData");
            
            using (MemoryStream msDecrypt = new MemoryStream(cipherData))
            {
                ICryptoTransform decryptor = _CAPI ? _aes.CreateDecryptor() : _decryptor;
                using (CryptoStream cs = new CryptoStream(msDecrypt, decryptor,
                                                          CryptoStreamMode.Read))
                {
                    int len = cipherData.Length;
                    byte[] buff=new byte[len];
                    realLength=cs.Read(buff, 0, buff.Length);
                    
                    return buff;
                }
            }
        }

        [Obsolete]
        public byte[] Decrypt2(byte[] cipherData)
        {
            if (cipherData == null || cipherData.Length <= 0)
                throw new ArgumentNullException("cipherData");

            using (MemoryStream msDecrypt = new MemoryStream(cipherData))
            {
                ICryptoTransform decryptor = _CAPI ? _aes.CreateDecryptor() : _decryptor;
                using (CryptoStream cs = new CryptoStream(msDecrypt, decryptor,
                                                          CryptoStreamMode.Read))
                {
                    int len = cipherData.Length;
                    byte[] buff = new byte[len];
                    int realLength = cs.Read(buff, 0, buff.Length);

                    byte[] res=new byte[realLength];//realLength<len, 16|len 
                    buff.CopyTo(res, realLength);
                    return res;
                }
            }
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
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, _encryptor,
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
                ICryptoTransform decryptor = _CAPI ? _aes.CreateDecryptor() : _decryptor;
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor,
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

        public void Encrypt(byte[] byteArray, Stream sEncrypted)//sEncrypted will be disposed on exit
        {
            if (byteArray == null || byteArray.Length <= 0)
                throw new ArgumentNullException("byteArray");

            using (CryptoStream csEncrypt = new CryptoStream(sEncrypted, _encryptor,
                                                             CryptoStreamMode.Write))
            //<-- plain data
            {
                csEncrypt.Write(byteArray, 0, byteArray.Length);
                //csEncrypt.FlushFinalBlock(); //No need as csEncrypt is closed after using
            }

        }

        public byte[] Decrypt(Stream sCrypted)//need maintenance
        {
            if (sCrypted == null || !sCrypted.CanRead)
                throw new ArgumentNullException("sCrypted");

            byte[] res = null;

            using (CryptoStream csDecrypt = new CryptoStream(sCrypted, _decryptor,
                                                             CryptoStreamMode.Read))
            {
                long len = sCrypted.Length;
                res = new byte[len];
                csDecrypt.Read(res, 0, res.Length);
            }

            return res;
        }

        public byte[] Decrypt(Stream sCrypted, int cryptLen, out int decrLen)
        {
            if (sCrypted == null || !sCrypted.CanRead)
                throw new ArgumentNullException("sCrypted");

            byte[] res = null;

            using (CryptoStream csDecrypt = new CryptoStream(sCrypted, _decryptor,
                                                             CryptoStreamMode.Read))
            {
                res = new byte[cryptLen];
                decrLen=csDecrypt.Read(res, 0, res.Length);
            }

            return res;
        }

        #endregion //Enc to Stream, dec from Stream
    }
}
