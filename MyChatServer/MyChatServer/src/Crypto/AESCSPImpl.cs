using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.Security.Cryptography
{
    public class AESCSPImpl
    {
        #region Fields

        //byte[] Key;//32 bytes
        //byte[] IV; //16 bytes
        public AesCryptoServiceProvider aes = null;        

        #endregion

        #region Constructors, Destructors

        public AESCSPImpl()
        {
            //byte[] salt = Encoding.Default.GetBytes("abcdefgh");
            //Rfc2898DeriveBytes keyGenerator = new Rfc2898DeriveBytes("demo", salt);
            //byte[] key = keyGenerator.GetBytes(16);
            //byte[] iv = keyGenerator.GetBytes(16);
            
            aes = new AesCryptoServiceProvider();
            //aes.Key = key;
            //aes.IV = iv;            
        }

        public AESCSPImpl(byte[] newKey, byte[] newIV)
        {
            if (newKey == null || newKey.Length <= 0)
                throw new ArgumentNullException("newKey");
            if (newIV == null || newIV.Length <= 0)
                throw new ArgumentNullException("newIV");            
            aes = new AesCryptoServiceProvider();
            aes.Key = newKey;
            aes.IV = newIV;            
        }

        public void Clear()
        {
            if (aes != null)
                aes.Clear();
        }          

        #endregion

        #region end/dec bytes2bytes

        public byte[] encrypt(byte[] byteArray)
        {
            // Check arguments.
            if (byteArray == null || byteArray.Length <= 0)
                throw new ArgumentNullException("byteArray");

            byte[] res = null;

            using (MemoryStream msEncrypt = new MemoryStream())//--> Encrypted data
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))//<-- plain data
                {
                    csEncrypt.Write(byteArray, 0, byteArray.Length);
                }

                res= msEncrypt.ToArray();
            }
            return res;
        }

        public byte[] decrypt(byte[] cipherText)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");

            byte[] res = null;

            using (MemoryStream msDecrypt = new MemoryStream())//--> Decrypted data
            {
                using (CryptoStream cs = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Write))//<-- Encrypted data
                {
                    cs.Write(cipherText, 0, cipherText.Length);
                }

                res= msDecrypt.ToArray();
            }

            return res;
        }

        #endregion

        #region enc str2bytes, dec bytes2str

        public byte[] encryptStr(string plainText)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");

            byte[] res = null;
            using (MemoryStream msEncrypt = new MemoryStream())//--> encrypted data
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))//<--plain data
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))//<-- plain data(string)  
                    {
                        swEncrypt.Write(plainText);
                    }
                }
                res = msEncrypt.ToArray();
            }
            return res;            
        }

        public string decryptStr(byte[] cipherText)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");

            string res = null;
            using (MemoryStream msDecrypt = new MemoryStream(cipherText))//<-- encrypted data
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))//--> decrypted data
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))//--> decrypted data (string)
                    {
                        res = srDecrypt.ReadToEnd();
                    }
                }
            }
            return res;            
        }

        #endregion
    }
}
