namespace Andriy.MyChat.Server
{
    using System;
    using System.Windows.Forms;

    public partial class ServerConfig : Form
    {
        public ServerConfig()
        {            
            this.InitializeComponent();
            this.nudPort.Value = Convert.ToDecimal(Properties.Settings.Default.Port);            
        }

        private void ServerConfigLoad(object sender, EventArgs e)
        {
            try
            {                
                ////fillDBConnectionTab();
                ////Program.loginsTableAdapterdef.Fill(this.chatServerDataSet.Logins);                     
            }
            catch (Exception ex)
            {
                Program.LogException(ex);
                Program.LogForm.LogException(new Exception("Error while connecting to DB. See logfile for details"));
                ////MessageBox.Show("Error while connecting to DB");
                ////Application.Exit();
            }
        }

        ////private void fillDBConnectionTab()
        ////{
        ////    System.Data.SqlClient.SqlConnectionStringBuilder builder = 
        ////        new System.Data.SqlClient.SqlConnectionStringBuilder(
        ////            Program.LoginsTableAdapterdef.Connection.ConnectionString);            
            
        ////    this.tbDataSource.Text=builder["Data Source"].ToString();
        ////    this.tbInitCat.Text=builder["Initial Catalog"].ToString();
        ////    this.tbUser.Text = builder["User ID"].ToString();
        ////    this.tbPass.Text = builder["Password"].ToString();
        ////    this.tbConfPass.Text=this.tbPass.Text;
        ////}

        private void BUpdateClick(object sender, EventArgs e)
        {
        }

        private void BRefreshClick(object sender, EventArgs e)
        {
        }     

        private void BApplyClick(object sender, EventArgs e)
        {
            if (this.tbConfPass.Text == this.tbPass.Text)
            {
                Properties.Settings.Default.Port = Convert.ToInt32(this.nudPort.Value);

                var builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
                builder["Data Source"] = this.tbDataSource.Text;
                ////builder["Integrated Security"] = true;
                builder["Initial Catalog"] = this.tbInitCat.Text;
                builder["User ID"] = this.tbUser.Text;
                builder["Password"] = this.tbPass.Text;

                ////Properties.Settings.Default.String1 = Crypto.Crypto1.encStrDef(builder.ConnectionString);
                Properties.Settings.Default.Save();
            }
            else
            {                
                MessageBox.Show(@"DB Connection: passwords do not match");
            }
        }

        #region User Interaction

        private void NotifyIcon1Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                this.ShowMe();
            }
        }

        private void ShowMe()
        {
            ////if (this.WindowState == FormWindowState.Minimized)
            ////    this.WindowState = FormWindowState.Normal;
            if (!this.Visible)
            {
                this.Show();
            }
                
            this.Activate();
        }

        private void TsmiConfigClick(object sender, EventArgs e)
        {
            this.ShowMe();
        }

        private void TsmiLogClick(object sender, EventArgs e)
        {
            Program.LogForm.showMe();
        }

        private void TsmiExitClick(object sender, EventArgs e)
        {
            this.StopServerAndExit();
        }

        private void ServerConfigFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                this.Hide();
            }            
        }

        private void BExitClick(object sender, EventArgs e)
        {
            this.StopServerAndExit();
        }
 
        private void StopServerAndExit()
        {
            Program.Server.Stop();
            Application.Exit();
        }

        #endregion
    }
}
