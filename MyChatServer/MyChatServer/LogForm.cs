using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace MyChatServer
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
        }

        public void LogEvent(string msg)
        {
            if (null != msg)
            {
                tbLog.AppendText(string.Format(CultureInfo.InvariantCulture, "{1} {2}", Environment.NewLine,
                    DateTime.Now, msg));
                showMe();
            }
        }

        public void LogException(Exception ex)
        {
            if (null != ex)
            {
                tbLog.AppendText(string.Format(CultureInfo.InvariantCulture, "{1} ERROR{0}{2}", Environment.NewLine,
                    DateTime.Now, ex));
                showMe();
            }
        }

        public void showMe()
        {
            //if (WindowState == FormWindowState.Minimized)
            //    WindowState = FormWindowState.Normal;
            if (Visible == false)
                this.Show();
            this.Activate();
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                //this.WindowState = FormWindowState.Minimized;
                Hide();
            }
            else
            {
                //
            }
        }
        
    }
}
