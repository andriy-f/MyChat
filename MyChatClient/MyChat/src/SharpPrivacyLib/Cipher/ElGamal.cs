using System;
using System.Security.Cryptography;
using SharpPrivacy.SharpPrivacyLib.Cipher.Math;

namespace MyChatServer.Crypto {
	
	
	/// <summary>
	/// This class is used for creating ElGamal keys, encrypting and decrypting,
	/// and signing and verifying with the ElGamal algorithm.
	/// </summary>
	/// <remarks>
	/// This class is used for creating ElGamal keys, encrypting and decrypting,
	/// and signing and verifying with the ElGamal algorithm.
	/// </remarks>
    public class ElGamal
    {

        #region Key stricts

        public struct EG_Public_Key
        {
            public BigInteger p;	    /* prime */
            public BigInteger g;	    /* group generator */
            public BigInteger y;	    /* g^x mod p */
        }

        public struct EG_Secret_Key
        {
            public BigInteger p;	    /* prime */
            public BigInteger g;	    /* group generator */
            public BigInteger y;	    /* g^x mod p */
            public BigInteger x;	    /* secret exponent */
        }

        #endregion

        public static EG_Public_Key secKey2Pub(EG_Secret_Key esk)
        {
            EG_Public_Key res;
            res.p = esk.p;
            res.g = esk.g;
            res.y = esk.y;
            return res;
        }

        #region Formatting

        public static Byte[] formatEGSecrKey(Byte tag, EG_Secret_Key esk)
        {
            int hdrSize = 17;
            Byte[] pB = esk.p.getBytes();
            Byte[] gB = esk.g.getBytes();
            Byte[] yB = esk.y.getBytes();
            Byte[] xB = esk.x.getBytes();
            int dataSize = pB.Length + gB.Length + yB.Length + xB.Length;
            //Header - 1+4+4+4+4         
            Byte[] msg = new Byte[hdrSize + dataSize];
            msg[0] = tag;
            BitConverter.GetBytes(pB.Length).CopyTo(msg, 1);
            BitConverter.GetBytes(gB.Length).CopyTo(msg, 5);
            BitConverter.GetBytes(yB.Length).CopyTo(msg, 9);
            BitConverter.GetBytes(xB.Length).CopyTo(msg, 13);
            //data
            int itr = hdrSize;
            pB.CopyTo(msg, itr);
            itr += pB.Length;
            gB.CopyTo(msg, itr);
            itr += gB.Length;
            yB.CopyTo(msg, itr);
            itr += yB.Length;
            xB.CopyTo(msg, itr);
            //itr += xB.Length;
            return msg;
        }

        public static Byte[] formatEGPubKey(Byte tag, EG_Public_Key epk)
        {
            int hdrSize = 13;
            Byte[] pB = epk.p.getBytes();
            Byte[] gB = epk.g.getBytes();
            Byte[] yB = epk.y.getBytes();            
            int dataSize = pB.Length + gB.Length + yB.Length;
            //Header - 1+4+4+4        
            Byte[] msg = new Byte[hdrSize + dataSize];
            msg[0] = tag;
            BitConverter.GetBytes(pB.Length).CopyTo(msg, 1);
            BitConverter.GetBytes(gB.Length).CopyTo(msg, 5);
            BitConverter.GetBytes(yB.Length).CopyTo(msg, 9);            
            //data
            int itr = hdrSize;
            pB.CopyTo(msg, itr);
            itr += pB.Length;
            gB.CopyTo(msg, itr);
            itr += gB.Length;
            yB.CopyTo(msg, itr);
            //itr += yB.Length;            
            return msg;
        }        

        #endregion

        #region Parsing

        public static EG_Secret_Key parseEGSecrKey(Byte[] bytes)
        {            
            //int hdrSize =17
            //Reading header
            int pBSize = BitConverter.ToInt32(bytes, 1);
            int gBSize = BitConverter.ToInt32(bytes, 5);
            int yBSize = BitConverter.ToInt32(bytes, 9);
            int xBSize = BitConverter.ToInt32(bytes, 13);
            //Reading data
            EG_Secret_Key esk;
            int itr = 17;

            byte[] tmpB=new byte[pBSize];
            Array.Copy(bytes, itr, tmpB, 0, pBSize);
            esk.p = new BigInteger(tmpB);
            itr += pBSize;

            tmpB = new byte[gBSize];
            Array.Copy(bytes, itr, tmpB, 0, gBSize);
            esk.g = new BigInteger(tmpB);
            itr += gBSize;

            tmpB = new byte[yBSize];
            Array.Copy(bytes, itr, tmpB, 0, yBSize);
            esk.y = new BigInteger(tmpB);
            itr += yBSize;

            tmpB = new byte[xBSize];
            Array.Copy(bytes, itr, tmpB, 0, xBSize);
            esk.x = new BigInteger(tmpB);
            //itr += xBSize;

            return esk;
        }

        public static EG_Public_Key parseEGPubKey(Byte[] bytes)
        {
            //int hdrSize =13
            //Reading header
            int pBSize = BitConverter.ToInt32(bytes, 1);
            int gBSize = BitConverter.ToInt32(bytes, 5);
            int yBSize = BitConverter.ToInt32(bytes, 9);            
            //Reading data
            EG_Public_Key epk;
            int itr = 13;

            byte[] tmpB = new byte[pBSize];
            Array.Copy(bytes, itr, tmpB, 0, pBSize);
            epk.p = new BigInteger(tmpB);
            itr += pBSize;

            tmpB = new byte[gBSize];
            Array.Copy(bytes, itr, tmpB, 0, gBSize);
            epk.g = new BigInteger(tmpB);
            itr += gBSize;

            tmpB = new byte[yBSize];
            Array.Copy(bytes, itr, tmpB, 0, yBSize);
            epk.y = new BigInteger(tmpB);
            //itr += yBSize;            

            return epk;
        }

        #endregion        

        #region Signing & Verification (Custom)

        //header: flag+tosignSize+signPart1Size+signPart2size=1+4+4+4=13
        //data: tosign+sign1+sign2
        public static Byte[] formatSignedPackege(Byte flag, Byte[] tosignB, HashAlgorithm halg, EG_Secret_Key esk)
        {
            Byte[] hash = halg.ComputeHash(tosignB);
            BigInteger[] sign = Sign(new BigInteger(hash), esk);
            Byte[] signP1B = sign[0].getBytes(),
                   signP2B = sign[1].getBytes();

            int hdrSize = 13,
                tosignS = tosignB.Length,
                signP1S = signP1B.Length,
                signP2S = signP2B.Length;

            Byte[] msg = new Byte[hdrSize + tosignS + signP1S + signP2S];

            //Format header
            BitConverter.GetBytes(tosignS).CopyTo(msg, 1);
            BitConverter.GetBytes(signP1S).CopyTo(msg, 5);
            BitConverter.GetBytes(signP2S).CopyTo(msg, 9);
            //Format data            
            int itr = hdrSize;
            tosignB.CopyTo(msg, itr);
            itr += tosignS;
            signP1B.CopyTo(msg, itr);
            itr += signP1S;
            signP2B.CopyTo(msg, itr);
            //itr += yB.Length;            
            return msg;
        }

        //header: flag+tosignSize+signPart1Size+signPart2size=1+4+4+4=13
        //data: tosign+sign1+sign2
        public static bool verifySignedPackege(Byte flag, Byte[] msg, HashAlgorithm halg, EG_Public_Key epk)
        {
            try
            {
                int
                    tosignS = BitConverter.ToInt32(msg, 1),
                    signP1S = BitConverter.ToInt32(msg, 5),
                    signP2S = BitConverter.ToInt32(msg, 9); 
                
                int itr = 13;
                //signed data
                byte[] tmpB=new byte[tosignS];                
                Array.Copy(msg, itr, tmpB, 0, tosignS);
                Byte[] hash=halg.ComputeHash(tmpB);
                BigInteger biHash= new BigInteger(hash);
                itr += tosignS;
                //sign
                BigInteger[] biSign=new BigInteger[2];
                //Part1
                tmpB = new byte[signP1S];
                Array.Copy(msg, itr, tmpB, 0, signP1S);
                biSign[0] = new BigInteger(tmpB);
                itr += signP1S;
                //Part2
                tmpB = new byte[signP2S];
                Array.Copy(msg, itr, tmpB, 0, signP2S);
                biSign[1] = new BigInteger(tmpB);
                itr += signP2S;

                return Verify(biSign, biHash, epk);
                
            }
            catch (Exception)
            {
                return false;
            }            
        }

        #endregion

        public static EG_Secret_Key GenerateKey(int nBits)
        {
            BigInteger q = new BigInteger();
            BigInteger p;
            BigInteger g;
            BigInteger gPowTwo;
            BigInteger gPowQ;
            EG_Secret_Key eskKey = new EG_Secret_Key();
            /*
            // construct a prime p = 2q + 1
            do {
                q = BigInteger.genRandom(nBits - 1);
                System.Windows.Forms.Application.DoEvents();
                p = (2*q) + 1;
            } while ((!p.isProbablePrime()) || (!q.isProbablePrime()));
            */
            q = BigInteger.genPseudoPrime(nBits - 1);
            p = BigInteger.genPseudoPrime(nBits);
            // find a generator
            BigInteger bi2 = new BigInteger(2);
            do
            {
                g = BigInteger.genRandom(nBits - 1);
                gPowTwo = g.modPow(bi2, p);
                gPowQ = g.modPow(q, p);
            }
            while ((gPowTwo == 1) || (gPowQ == 1));

            BigInteger x;
            do
            {                
                x = BigInteger.genRandom(nBits);
            } while (x >= p - 1);
            BigInteger y = g.modPow(x, p);
            eskKey.p = p;
            eskKey.g = g;
            eskKey.x = x;
            eskKey.y = y;
            return eskKey;
        }

        /// <summary>
        /// Secret key operation. Decrypts biCipher with the keydata
        /// in the given secret key packet.
        /// </summary>
        /// <param name="biInput">The ciphertext that is about to
        /// be decrypted</param>
        /// <param name="eskKey">The secret key packet with the key
        /// material for the decryption</param>
        /// <returns>The decrypted ciphertext.</returns>
        /// <remarks>No remarks.</remarks>
        public BigInteger Decrypt(BigInteger[] biInput, EG_Secret_Key eskKey)
        {
            if (biInput.Length != 2)
                throw new ArgumentException("biInput is not an ElGamal encrypted Packet");
            BigInteger B = biInput[0];
            BigInteger c = biInput[1];
            BigInteger z = B.modPow(eskKey.x, eskKey.p).modInverse(eskKey.p);
            BigInteger output = (z * c) % eskKey.p;
            return output;
        }

        /// <summary>
        /// Public key operation. Encrypts biInput with the keydata
        /// in the given public key packet.
        /// </summary>
        /// <param name="biInput">The plaintext that is about to
        /// be encrypted</param>
        /// <param name="epkKey">The public key packet with the key
        /// material for the encryption</param>
        /// <returns>The encrypted ciphertext.</returns>
        /// <remarks>No remarks.</remarks>
        public BigInteger[] Encrypt(BigInteger biInput, EG_Public_Key epkKey)
        {
            //Random number needed for encryption
            BigInteger k = BigInteger.genRandom(epkKey.p.bitCount() - 1);
            int d = 0;
            while (k > (epkKey.p - 1))
            {
                //d++;
                k = BigInteger.genRandom(epkKey.p.bitCount() - 1 - d);
            }
            BigInteger B = epkKey.g.modPow(k, epkKey.p);
            BigInteger c = epkKey.y.modPow(k, epkKey.p);
            c = (biInput * c) % epkKey.p;
            //BigInteger c = (biInput * epkKey.y.modPow(k, epkKey.p)) % epkKey.p;
            BigInteger[] biOutput = new BigInteger[2];
            biOutput[0] = B;
            biOutput[1] = c;
            return biOutput;

        }

        public static BigInteger[] Sign(BigInteger biHash, EG_Secret_Key eskKey)
        {
            BigInteger pmin1 = eskKey.p - 1;
            BigInteger s1, s2;
            int d = 0;
            do
            {
                BigInteger k = BigInteger.genRandom(pmin1.bitCount() - 1);
                while (k >= pmin1 || k.gcd(pmin1) != 1)
                {
                    //d++;
                    k = BigInteger.genRandom(pmin1.bitCount() - 1 - d);
                }
                s1 = eskKey.g.modPow(k, eskKey.p);
                BigInteger km = k.modInverse(pmin1);

                BigInteger xMs1 = (eskKey.x * s1) % pmin1;
                //BigInteger tHash = new BigInteger(biHash);
                while (biHash < xMs1)
                    biHash += pmin1;
                s2 = ((biHash - xMs1) * km) % pmin1;
            } while (s2 == 0);

            //Check
            //if ((eskKey.x * s1 + k * s2 - biHash) % pmin1 != 0) throw new Exception("nea");

            BigInteger[] biOutput = new BigInteger[2];
            biOutput[0] = s1;
            biOutput[1] = s2;
            return biOutput;
        }

        public static bool Verify(BigInteger[] biSignature, BigInteger biHash, EG_Public_Key epkKey)
        {
            if (biSignature.Length != 2)
                throw new ArgumentException("biSignature is not an ElGamal encrypted Packet");
            BigInteger p=epkKey.p;

            BigInteger s1 = biSignature[0];
            BigInteger s2 = biSignature[1];

            BigInteger gH = epkKey.g.modPow(biHash, p);

            BigInteger ch = (epkKey.y.modPow(s1, p)*s1.modPow(s2, p)) % p;
            return (gH==ch);
        }
    }
	
}
