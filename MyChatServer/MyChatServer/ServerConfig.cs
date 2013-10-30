using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace MyChatServer
{
    public partial class ServerConfig : Form
    {
        public ServerConfig()
        {            
            InitializeComponent();
            nudPort.Value = Convert.ToDecimal(Properties.Settings.Default.Port);            
        }

        private void ServerConfig_Load(object sender, EventArgs e)
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
                //MessageBox.Show("Error while connecting to DB");
                //Application.Exit();
            }
        }

        private void fillDBConnectionTab()
        {
            System.Data.SqlClient.SqlConnectionStringBuilder builder = 
                new System.Data.SqlClient.SqlConnectionStringBuilder(
                    Program.LoginsTableAdapterdef.Connection.ConnectionString);            
            
            tbDataSource.Text=builder["Data Source"].ToString();
            tbInitCat.Text=builder["Initial Catalog"].ToString();
            tbUser.Text = builder["User ID"].ToString();
            tbPass.Text = builder["Password"].ToString();
            tbConfPass.Text=tbPass.Text;
        }

        private void bUpdate_Click(object sender, EventArgs e)
        {
            Program.LoginsTableAdapterdef.Update(chatServerDataSet);            
        }

        private void bRefresh_Click(object sender, EventArgs e)
        {
            Program.LoginsTableAdapterdef.Fill(this.chatServerDataSet.Logins);
        }     

        private void bApply_Click(object sender, EventArgs e)
        {
            if (tbConfPass.Text == tbPass.Text)
            {
                Properties.Settings.Default.Port = Convert.ToInt32(nudPort.Value);

                System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
                builder["Data Source"] = tbDataSource.Text;
                //builder["Integrated Security"] = true;
                builder["Initial Catalog"] = tbInitCat.Text;
                builder["User ID"] = tbUser.Text;
                builder["Password"] = tbPass.Text;

                Program.LoginsTableAdapterdef.Connection.ConnectionString = builder.ConnectionString;

                Program.SaveConStr(builder.ConnectionString);
                //Properties.Settings.Default.String1 = Crypto.Crypto1.encStrDef(builder.ConnectionString);
                Properties.Settings.Default.Save();
            }
            else
            {                
                MessageBox.Show("DB Connection: passwords do not match");
            }
        }

        #region User Interaction

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Left)
            {
                showMe();
            }
        }

        private void showMe()
        {
            //if (this.WindowState == FormWindowState.Minimized)
            //    this.WindowState = FormWindowState.Normal;
            if (!Visible)
                Show();
            this.Activate();
        }

        private void tsmiConfig_Click(object sender, EventArgs e)
        {
            showMe();
        }

        private void tsmiLog_Click(object sender, EventArgs e)
        {
            Program.LogForm.showMe();
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            stopServerAndExit();
        }

        private void ServerConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                this.Hide();
            }            
        }

        private void bExit_Click(object sender, EventArgs e)
        {
            stopServerAndExit();
        }
 
        private void stopServerAndExit()
        {
            ChatServer.Finish();
            Application.Exit();
        }

        #endregion
    }
}
