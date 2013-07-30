using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text
{
    /// <summary>
    /// Class for getting string representation of byte arrays (in hex format)
    /// </summary>
    public static class HexRep
    {
        #region bytes2String
        
        public static string ToString(byte[] bytes)// like BitConverter.ToString() --Better
        {
            if (bytes != null)
            {
                if (bytes.Length > 0)
                {
                    int len = bytes.Length;
                    StringBuilder sBuilder = new StringBuilder(2 * len);
                    sBuilder.Length = 2 * len;
                    int sbi = 0;
                    for (int i = 0; i < len; i++)
                    {
                        sBuilder[sbi] = byte2char(bytes[i] / 16);//sixteens
                        sBuilder[sbi + 1] = byte2char(bytes[i] % 16);//ones
                        sbi += 2;
                    }
                    return sBuilder.ToString();
                }
                else return "";
            }
            else return "<null>";
        }

        //public static string ToString2(byte[] bytes)//Slower
        //{
        //    if (bytes != null)
        //    {
        //        int len = bytes.Length;
        //        StringBuilder sBuilder = new StringBuilder(len * 2);
        //        for (int i = 0; i < len; i++)
        //            sBuilder.Append(bytes[i].ToString("X2"));
        //        return sBuilder.ToString();
        //    }
        //    else return "<null>";
        //}

        static char byte2char(int bt)//bt in [0, 15]
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
                default: throw new ArgumentOutOfRangeException("b1", "bt must be in range [0..15]");
            }
        }

        #endregion

        #region String representation to bytes

        public static byte[] ToBytes(string str)
        {
            try
            {
                if (str != null)
                {
                    if (str.Length > 0)
                    {
                        if (str.Length % 2 == 0)
                        {
                            int len = str.Length / 2;
                            byte[] res = new byte[len];
                            int strIdx = 0;
                            for (int i = 0; i < len; i++)
                            {
                                //res[i] = Convert.ToByte(16 * hexChar2dec(str[strIdx]) + hexChar2dec(str[strIdx + 1]));
                                res[i] = (byte)(16 * hexChar2dec(str[strIdx]) + hexChar2dec(str[strIdx + 1]));
                                strIdx += 2;
                            }
                            return res;
                        }
                        else throw new ArgumentException("String str is not valid hex representation (Length cannot be divided by 2)", "str");
                    }
                    else throw new ArgumentException("String str is empty", "str");
                }
                else throw new ArgumentException("String str is null", "str");
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting bytes from string hex representation", ex);
            }
        }

        //public static byte[] ToBytes2(string str)//Slower
        //{
        //    try
        //    {
        //        if (str != null)
        //        {
        //            if (str.Length % 2 == 0)
        //            {
        //                int len = str.Length / 2;
        //                byte[] res = new byte[len];
        //                int strIdx = 0;
        //                for (int i = 0; i < len; i++)
        //                {
        //                    res[i] = byte.Parse(str.Substring(strIdx, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
        //                    strIdx += 2;
        //                }
        //                return res;
        //            }
        //            else
        //                throw new Exception("Length cannot be divided by 2");
        //        }
        //        else return null;//throw new Exception
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error while getting bytes from string hex representation", ex);
        //    }
        //}

        static int hexChar2dec(char c)
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
                default: throw new Exception("Not valid hex char");
            }
        }

        #endregion
    }
}
