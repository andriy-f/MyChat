namespace Andriy.MyChat.Server
{
    using System;
    using System.Globalization;
    using System.Windows.Forms;

    public partial class LogForm : Form
    {
        public LogForm()
        {
            this.InitializeComponent();
        }

        public void LogEvent(string msg)
        {
            if (null != msg)
            {
                this.tbLog.AppendText(string.Format(CultureInfo.InvariantCulture, "{1} {2}", Environment.NewLine,
                    DateTime.Now, msg));
                this.showMe();
            }
        }

        public void LogException(Exception ex)
        {
            if (null != ex)
            {
                this.tbLog.AppendText(string.Format(CultureInfo.InvariantCulture, "{1} ERROR{0}{2}", Environment.NewLine,
                    DateTime.Now, ex));
                this.showMe();
            }
        }

        public void showMe()
        {
            //if (WindowState == FormWindowState.Minimized)
            //    WindowState = FormWindowState.Normal;
            if (this.Visible == false)
                this.Show();
            this.Activate();
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                //this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
            else
            {
                //
            }
        }
        
    }
}
