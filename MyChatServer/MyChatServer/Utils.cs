using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyChatServer
{
    using System.Windows.Forms;

    public static class Utils
    {
        private static string getIntBytes(byte[] bts)
        {
            StringBuilder sb = new StringBuilder(String.Format("{0}", bts.Length));
            foreach (byte bt in bts)
                sb.Append(String.Format("{0}, ", bt));
            return sb.ToString();
        }

        public static void defMsgBox(string msg)
        {
            MessageBox.Show(msg, "Chat Server");
        }
    }
}
