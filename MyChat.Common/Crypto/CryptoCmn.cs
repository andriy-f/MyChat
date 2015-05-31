namespace Andriy.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    public class MyRandoms
    {
        ////private readonly Random simpleRandom = new Random();
        
        ////private readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        
        private readonly Org.BouncyCastle.Security.SecureRandom secureRandom = new Org.BouncyCastle.Security.SecureRandom();
        
        private static readonly MyRandoms Randoms = new MyRandoms();

        /// <summary>
        /// Generates random byte array of set length
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>random byte array of set length</returns>
        ////public byte[] genRandomBytes(int len)
        ////{
        ////    var bytes = new byte[len];
        ////    this.simpleRandom.NextBytes(bytes);
        ////    return bytes;
        ////}

        /// <summary>
        /// Generates secure random byte array of set length using .NET
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>random byte array of set length</returns>
        ////public byte[] genSecureRandomBytesNative(int len)
        ////{
        ////    var bytes = new byte[len];
        ////    this.rngCsp.GetBytes(bytes);
        ////    return bytes;
        ////}

        /// <summary>
        /// Generates secure random byte array of set length using Bouncy Castle
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>secure random byte array of set length</returns>
        public byte[] GenSecureRandomBytes(int len)
        {
            var bytes = new byte[len];
            this.secureRandom.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generates secure random byte array of set length using Bouncy Castle
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>secure random byte array of set length</returns>
        public static byte[] GenerateSecureRandomBytes(int len)
        {
            return Randoms.GenSecureRandomBytes(len);
        }
    }
}
