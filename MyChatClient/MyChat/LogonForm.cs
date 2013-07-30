using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;

namespace MyChat
{
    public partial class LogonForm : Form
    {
        public LogonForm()
        {
            InitializeComponent();

            //System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

            tbLogin.Text = "user1";
            tbPass.Text = "qwe`123";

            cbServer.Text = MyChat.Properties.Settings.Default.DefServer;
            nudPort.Value=Convert.ToDecimal(MyChat.Properties.Settings.Default.DefPort);
        }

        private void bLogin_Click(object sender, EventArgs e)
        {
            ChatClient.init(cbServer.Text, Convert.ToInt32(nudPort.Value), tbLogin.Text, tbPass.Text);
            if (performAuth() && ChatClient.performAgreement())
            {
                performLogon();
            }
        }

        private void bNewAcc_Click(object sender, EventArgs e)
        {
            if (Width == 320)
            {
                Width = 660;
                bNewAcc.Text="New Account <<";
            }
            else
            {
                Width = 320;
                bNewAcc.Text = "New Account >>";
            }
        }

        private void bRegister_Click(object sender, EventArgs e)
        {
            if (tbRegLogin.Text != "" && tbRegPass.Text != "" && tbRegPass.Text == tbRegConf.Text)
            {
                ChatClient.init(cbServer.Text, Convert.ToInt32(nudPort.Value), tbRegLogin.Text, tbRegPass.Text);
                if (performAuth() && ChatClient.performAgreement())
                {
                    
                    performReg();
                }
            }
            else MessageBox.Show("Invalid registration data");
        }

        private void bCancel_Click(object sender, EventArgs e)
        { 
            Close(); 
        }

        private bool performAuth()
        {
            int rs = ChatClient.performAuth();
            switch (rs)
            {
                case 0:
                    return true;
                case 1:
                    MessageBox.Show("Server is not valid");
                    return false;
                case 2:
                    MessageBox.Show("Error while authentificating");
                    return false;
                case 3:
                    MessageBox.Show("Invalid response from server");
                    return false;
                case 4:
                    MessageBox.Show("Network Socket exception");
                    return false;
                default://Error
                    MessageBox.Show("Logon error");
                    return false;
            }            
        }

        private void performLogon()
        {
            int rs = ChatClient.performLogonDef();
            switch (rs)
            {
                case 0://Success                    
                    ChatClient.startListener();
                    SelRoomForm selroomform1 = new SelRoomForm();
                    this.Hide();
                    selroomform1.Show();
                    break;
                case 1://Already logged on
                    MessageBox.Show("Already logged on");
                    break;
                case 2://Invalid login/pass                    
                    MessageBox.Show("Invalid login/pass");
                    break;
                case 3:
                    MessageBox.Show("Invalid response from server");
                    break;
                case 4:
                    MessageBox.Show("Network Socket exception");
                    break;
                default://Error
                    MessageBox.Show("Logon error");
                    break;
            }
        }

        private void performReg()
        {
            switch (ChatClient.performRegDef(false))
            {
                case 0:
                    MessageBox.Show(String.Format("Registration success: User '{0}' is now registered", tbRegLogin.Text));
                    break;
                case 1:
                    MessageBox.Show(String.Format("Registration failed: User '{0}' already registered", tbRegLogin.Text));
                    break;
                default:
                    MessageBox.Show(String.Format("Registration failed"));
                    break;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }     
    }    
}
