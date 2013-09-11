using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Security.Cryptography
{
    internal class MyRandoms
    {
        private Random _simpleRandom = new Random();
        private RNGCryptoServiceProvider _rngCsp = new RNGCryptoServiceProvider();
        private Org.BouncyCastle.Security.SecureRandom _secureRandom = new Org.BouncyCastle.Security.SecureRandom();
        
        /// <summary>
        /// Generates random byte array of set length
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>random byte array of set length</returns>
        public byte[] genRandomBytes(int len)
        {
            byte[] bytes = new byte[len];
            _simpleRandom.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generates secure random byte array of set length using .NET
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>random byte array of set length</returns>
        public byte[] genSecureRandomBytesNative(int len)
        {
            byte[] bytes = new byte[len];
            _rngCsp.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generates secure random byte array of set length using Bouncy Castle
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>secure random byte array of set length</returns>
        public byte[] genSecureRandomBytes(int len)
        {
            byte[] bytes = new byte[len];
            _secureRandom.NextBytes(bytes);
            return bytes;
        }
    }
}
