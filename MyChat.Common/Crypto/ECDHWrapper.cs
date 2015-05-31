namespace Andriy.Security.Cryptography
{
    using System;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Agreement;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Security;
    
    public class ECDHWrapper
    {
        #region Fields

        private BigInteger p, g;         

        int agrlen;

        DHPublicKeyParameters pu1;
        DHPrivateKeyParameters pv1;

        DHAgreement e1;
        BigInteger m1;

        byte[] pubdata;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newagrlen">agreement length in bytes. Valid values are 32, 96, 128</param>
        public ECDHWrapper(int newagrlen)
        {
            DHKeyPairGenerator kpGen;
            this.initPG(newagrlen);
            kpGen = this.getDHKeyPairGenerator(this.g, this.p);
            this.agrlen = newagrlen;

            AsymmetricCipherKeyPair pair = kpGen.GenerateKeyPair();

            this.pu1 = (DHPublicKeyParameters)pair.Public;
            this.pv1 = (DHPrivateKeyParameters)pair.Private;

            this.e1 = new DHAgreement();
            this.e1.Init(new ParametersWithRandom(this.pv1, new SecureRandom()));
            this.m1 = this.e1.CalculateMessage();

            this.pubdata = this.getPubData();
        }

        private void initPG(int newagrlen)
        {
            switch (newagrlen)
            {
                case 32:
                    //512 bits
                    this.p=new BigInteger("9494fec095f3b85ee286542b3836fc81a5dd0a0349b4c239dd38744d488cf8e31db8bcb7d33b41abb9e5a33cca9144b1cef332c94bf0573bf047a3aca98cdf3b", 16);
                    this.g=new BigInteger("153d5d6172adb43045b68ae8e1de1070b6137005686d29d3d73a7749199681ee5b212c9b96bfdcfa5b20cd5e3fd2044895d609cf9b410b7a0f12ca1cb9a428cc", 16);
                    break;
                case 96:
                    //768 bits
                    this.p=new BigInteger("8c9dd223debed1b80103b8b309715be009d48860ed5ae9b9d5d8159508efd802e3ad4501a7f7e1cfec78844489148cd72da24b21eddd01aa624291c48393e277cfc529e37075eccef957f3616f962d15b44aeab4039d01b817fde9eaa12fd73f", 16);
                    this.g=new BigInteger("7c240073c1316c621df461b71ebb0cdcc90a6e5527e5e126633d131f87461c4dc4afc60c2cb0f053b6758871489a69613e2a8b4c8acde23954c08c81cbd36132cfd64d69e4ed9f8e51ed6e516297206672d5c0a69135df0a5dcf010d289a9ca1", 16);
                    break;
                case 128:
                    //1024 bits
                    this.p=new BigInteger("a00e283b3c624e5b2b4d9fbc2653b5185d99499b00fd1bf244c6f0bb817b4d1c451b2958d62a0f8a38caef059fb5ecd25d75ed9af403f5b5bdab97a642902f824e3c13789fed95fa106ddfe0ff4a707c85e2eb77d49e68f2808bcea18ce128b178cd287c6bc00efa9a1ad2a673fe0dceace53166f75b81d6709d5f8af7c66bb7", 16);
                    this.g=new BigInteger("1db17639cdf96bc4eabba19454f0b7e5bd4e14862889a725c96eb61048dcd676ceb303d586e30f060dbafd8a571a39c4d823982117da5cc4e0f89c77388b7a08896362429b94a18a327604eb7ff227bffbc83459ade299e57b5f77b50fb045250934938efa145511166e3197373e1b5b1e52de713eb49792bedde722c6717abf", 16);
                    break;
                default:
                    throw new ArgumentException("Must be 32, 96 or 128", "newagrlen");
            }            
        }

        private Byte[] getPubData()
        {
            int hdrSize = 8;
            Byte[] bY = this.pu1.Y.ToByteArray();
            Byte[] bm1 = this.m1.ToByteArray();
            int dataSize = bY.Length + bm1.Length;
            //Header - 4+4       
            Byte[] msg = new Byte[hdrSize + dataSize];            
            BitConverter.GetBytes(bY.Length).CopyTo(msg, 0);
            BitConverter.GetBytes(bm1.Length).CopyTo(msg, 4);         
            //data
            int itr = hdrSize;
            bY.CopyTo(msg, itr);
            itr += bY.Length;
            bm1.CopyTo(msg, itr);
            return msg;
        }



        #region Calculate agreement

        public byte[] calcAgreement(Byte[] pubdata)
        {
            return this.calcAgreement(pubdata, this.agrlen);
        }

        public byte[] calcAgreement(Byte[] pubdata, int rbytes)//rbytes - return length in bytes
        {
            byte[] agr = this.calcAgreementDef(pubdata);
            if (agr.Length == rbytes)
                return agr;
            else
            {
                byte[] newres = new byte[rbytes];
                int lessLen = Math.Min(agr.Length, rbytes);
                for (int i = 0; i < lessLen; i++)
                    newres[i] = agr[i];
                for (int i = lessLen; i < rbytes; i++)
                    newres[i] = 0;
                return newres;
            }
        }

        public byte[] calcAgreementDef(Byte[] pubdata)
        {
            //int hdrSize =8
            //Reading header
            int bY2Size = BitConverter.ToInt32(pubdata, 0);
            int bm2Size = BitConverter.ToInt32(pubdata, 4);
            //Reading data
            int itr = 8;
            BigInteger Y2 = new BigInteger(pubdata, itr, bY2Size);
            itr += bY2Size;
            BigInteger m2 = new BigInteger(pubdata, itr, bm2Size);
            DHParameters newDHParameters = new DHParameters(this.p, this.g);
            DHPublicKeyParameters pu2 = new DHPublicKeyParameters(Y2, newDHParameters);
            return this.e1.CalculateAgreement(pu2, m2).ToByteArray();//Variable length (not necessary bits / 8)            
        }

        #endregion

        public byte[] PubData
        {
            get { return this.pubdata; }
        }

        private DHKeyPairGenerator getDHKeyPairGenerator(BigInteger g, BigInteger p)
        {
            DHParameters dhParams = new DHParameters(p, g);
            DHKeyGenerationParameters dhkgParams = new DHKeyGenerationParameters(new SecureRandom(), dhParams);
            DHKeyPairGenerator kpGen = new DHKeyPairGenerator();

            kpGen.Init(dhkgParams);

            return kpGen;
        }
    }
}
