namespace Andriy.MyChat.Server
{
    using System;
    using System.Text;

    /// <summary>
    /// Class for getting string representation of byte arrays (in hex format)
    /// </summary>
    public static class ConvertUtils
    {
        /// <summary>
        /// Convert byte array to hex representation. Similar to BitConverter.ToString()
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentNullException("bytes");
            }

            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        public static byte[] ToBytes(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException("Must not be null or empty", "str");
            }

            if (str.Length % 2 != 0)
            {
                throw new ArgumentException("Invalid hex string", "str");
            }

            try
            {
                int len = str.Length / 2;
                byte[] res = new byte[len];
                int strIdx = 0;
                for (int i = 0; i < len; i++)
                {
                    res[i] = (byte)(16 * HexChar2Dec(str[strIdx]) + HexChar2Dec(str[strIdx + 1]));
                    strIdx += 2;
                }

                return res;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting bytes from string hex representation", ex);
            }
        }

        /// <summary>
        /// cONVERT BYTE TO CHAR
        /// </summary>
        /// <param name="bt">in range [0-15]</param>
        /// <returns></returns>
        private static char Byte2Char(int bt)
        {
            switch (bt)
            {
                case 0: return '0';
                case 1: return '1';
                case 2: return '2';
                case 3: return '3';
                case 4: return '4';
                case 5: return '5';
                case 6: return '6';
                case 7: return '7';
                case 8: return '8';
                case 9: return '9';
                case 10: return 'A';
                case 11: return 'B';
                case 12: return 'C';
                case 13: return 'D';
                case 14: return 'E';
                case 15: return 'F';
                default: throw new ArgumentOutOfRangeException("bt", "Must be in range [0..15]");
            }
        }

        private static int HexChar2Dec(char c)
        {
            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a':
                case 'A': return 10;
                case 'b':
                case 'B': return 11;
                case 'c':
                case 'C': return 12;
                case 'd':
                case 'D': return 13;
                case 'e':
                case 'E': return 14;
                case 'f':
                case 'F': return 15;
                default: throw new ArgumentOutOfRangeException("c", "Must be [0-f] ([0-F])");
            }
        }

        public static bool ByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1 == null && a2 == null)
            {
                return true;
            }

            if ((a1 == null && a2 != null) || (a1 != null && a2 == null) || (a1.Length != a2.Length))
            {
                return false;
            }

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
