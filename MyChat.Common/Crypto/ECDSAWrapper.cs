namespace Andriy.Security.Cryptography
{
    using System;

    using Org.BouncyCastle.Asn1.Nist;
    using Org.BouncyCastle.Asn1.X9;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Signers;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Math.EC;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.Utilities.Encoders;

    public class ECDSAWrapper
    {
        #region Fields

        private ECCurve ecCurve;
        private ECDomainParameters parameters;
        private ECDsaSigner ecdsa = new ECDsaSigner();
        //ICipherParameters cipherParams;
        AsymmetricCipherKeyPair pair = null;

        #endregion

        #region Constructors and inits

        /// <summary>
        /// Create ECDSA and generates new key pair
        /// </summary>
        /// <param name="type">0 or 1 (1 faster)</param>
        /// <param name="forSign">if created for signing, otherwise for verifying</param>
        public ECDSAWrapper(int type, bool forSign)
        {
            try
            {
                this.initCurveandParams(type);

                SecureRandom random = new SecureRandom();
                ECKeyGenerationParameters genParam = new ECKeyGenerationParameters(this.parameters, random);
                ECKeyPairGenerator pGen = new ECKeyPairGenerator();
                pGen.Init(genParam);
                this.pair = pGen.GenerateKeyPair();

                if (forSign)
                    this.ecdsa.Init(true, new ParametersWithRandom(this.pair.Private, random));
                else
                    this.ecdsa.Init(false, this.pair.Public);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while creating ECDSA with new key pair", ex);
            }
        }

        /// <summary>
        /// Creates ECDSA from imported data
        /// </summary>
        /// <param name="type">0 or 1 (1 faster)</param>
        /// <param name="forSign">if created for signing, otherwise for verifying</param>
        /// <param name="import">Imporeted public or private key</param>
        public ECDSAWrapper(int type, bool forSign, byte[] import)
        {
            this.initCurveandParams(type);

            if (forSign)
            {
                try
                {
                    //import - D (BigInteger)
                    SecureRandom random = new SecureRandom();
                    BigInteger Drec = new BigInteger(import);
                    ECPrivateKeyParameters ecPrivImported = new ECPrivateKeyParameters(Drec, this.parameters);
                    ParametersWithRandom ecPrivImportedpwr = new ParametersWithRandom(ecPrivImported, random);
                    this.ecdsa.Init(true, ecPrivImportedpwr);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while creating ECDSAWrapper from import for signing", ex);
                }
            }
            else
            {
                try
                {
                    //import - Q (ECPoint)
                    ECPoint Qrec = this.ecCurve.DecodePoint(import);
                    ECPublicKeyParameters recPub = new ECPublicKeyParameters(Qrec, this.parameters);
                    this.ecdsa.Init(false, recPub);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while creating ECDSAWrapperfom import for verifying", ex);
                }
            }
        }

        private void initCurveandParams(int type)
        {
            switch (type)
            {
                case 0:
                    X9ECParameters p = NistNamedCurves.GetByName("P-521");
                    this.ecCurve = p.Curve;
                    this.parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
                    break;

                case 1:
                    this.ecCurve = new FpCurve(
                        new BigInteger("883423532389192164791648750360308885314476597252960362792450860609699839"), // q
                        new BigInteger("7fffffffffffffffffffffff7fffffffffff8000000000007ffffffffffc", 16), // a
                        new BigInteger("6b016c3bdcf18941d0d654921475ca71a9db2fb27d1d37796185c2942c0a", 16)); // b

                    this.parameters = new ECDomainParameters(
                        this.ecCurve,
                        this.ecCurve.DecodePoint(Hex.Decode("020ffa963cdca8816ccc33b8642bedf905c3d358573d3f27fbbd3b3cb9aaaf")), // G
                        new BigInteger("883423532389192164791648750360308884807550341691627752275345424702807307")); // n
                    break;

                default:
                    throw new ArgumentException("type must be 0 or 1", "type");
            }
        } 
        #endregion

        #region Signing

        public byte[] signHash(byte[] hash)
        {
            BigInteger[] sig = this.ecdsa.GenerateSignature(hash);

            //int hsz = 8;//header size 4+4
            //sign
            byte[] sig0 = sig[0].ToByteArray();
            byte[] sig1 = sig[1].ToByteArray();

            byte[] sign = new byte[8 + sig0.Length + sig1.Length];

            //format header
            BitConverter.GetBytes(sig0.Length).CopyTo(sign, 0);
            BitConverter.GetBytes(sig1.Length).CopyTo(sign, 4);

            sig0.CopyTo(sign, 8);
            sig1.CopyTo(sign, 8 + sig0.Length);

            return sign;
        }

        #endregion

        #region Verifying

        public bool verifyHash(byte[] hash, byte[] sign)
        {
            try
            {
                //int hsz = 8;//header size 4+4
                //sign
                int sig0sz = BitConverter.ToInt32(sign, 0);
                int sig1sz = BitConverter.ToInt32(sign, 4);

                byte[] sig0 = new byte[sig0sz];
                byte[] sig1 = new byte[sig1sz];

                Array.Copy(sign, 8, sig0, 0, sig0sz);
                Array.Copy(sign, 8 + sig0sz, sig1, 0, sig1sz);

                BigInteger r = new BigInteger(sig0);
                BigInteger s = new BigInteger(sig1);

                return this.ecdsa.VerifySignature(hash, r, s);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Export

        public byte[] exportPrivate()// Export Q (ECPoint)
        {
            if (this.pair != null)
            {
                ECPrivateKeyParameters ecpriv = (ECPrivateKeyParameters)this.pair.Private;
                return ecpriv.D.ToByteArray();
            }
            else throw new Exception("Cannot export private data (key pair is not new)");
        }

        public byte[] exportPublic()// Export Q (ECPoint)
        {
            if (this.pair != null)
            {
                ECPublicKeyParameters ecpub = (ECPublicKeyParameters)this.pair.Public;//parameters - const            
                return ecpub.Q.GetEncoded();
            }
            else throw new Exception("Cannot export public data (key pair is not new)");
        } 


        #endregion

        

    }
}
