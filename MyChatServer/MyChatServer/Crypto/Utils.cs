using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyChatServer.Crypto
{
    using CustomCrypto;

    class Utils
    {
        private static readonly byte[] StaticServerPrivKey =
        {
            71, 177, 16, 173, 145, 214, 65, 103, 205, 32, 107, 19, 241, 
            223, 113, 87, 172, 178, 195, 75, 171, 208, 130, 47, 94, 
            231, 207, 220, 175, 147
        };

        private static readonly byte[] StaticClientPubKey =
        {
            4, 99, 1, 55, 9, 242, 97, 187, 246, 226, 134, 61, 17, 155, 
            222, 10, 51, 13, 189, 232, 245, 186, 228, 228, 238, 99, 35, 
            125, 165, 38, 99, 67, 134, 36, 246, 134, 76, 217, 117, 135, 
            70, 63, 208, 9, 252, 2, 81, 227, 196, 2, 19, 112, 228, 245, 
            86, 190, 33, 150, 25, 166, 41
        };

        private static ECDSAWrapper clientVerifier; // checks with staticClientPubKey

        private static ECDSAWrapper serverSigner; // signs with staticServerPrivKey

        static Utils()
        {
            serverSigner = new ECDSAWrapper(1, true, StaticServerPrivKey);
            clientVerifier = new ECDSAWrapper(1, false, StaticClientPubKey);
        }

        public static ECDSAWrapper ClientVerifier
        {
            get
            {
                return clientVerifier;
            }
        }

        public static ECDSAWrapper ServerSigner
        {
            get
            {
                return serverSigner;
            }
        }
    }
}
