namespace Andriy.MyChat.Client
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    using global::MyChat.Client.Core;

    public partial class LogonForm : Form
    {
        private readonly ChatClient chatClient;

        public LogonForm(ChatClient chatClient)
        {
            this.InitializeComponent();

            ////System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;

            this.chatClient = chatClient;

            // TODO: remove after debug
            this.tbLogin.Text = @"user1";
            this.tbPass.Text = @"qwe`123";

            this.cbServer.Text = Properties.Settings.Default.DefServer;
            this.nudPort.Value = Convert.ToDecimal(Properties.Settings.Default.DefPort);
        }

        private void bLogin_Click(object sender, EventArgs e)
        {
            try
            {
                this.chatClient.Init(this.cbServer.Text, Convert.ToInt32(this.nudPort.Value), this.tbLogin.Text,
                    this.tbPass.Text);
                this.chatClient.ValidateItselfAndServer();
                this.chatClient.SetUpSecureChannel();
                this.performLogon();
            }
            catch (Exception)
            {
                MessageBox.Show(@"Error logging in");
            }
        }

        private void bNewAcc_Click(object sender, EventArgs e)
        {
            if (this.Width == 320)
            {
                this.Width = 660;
                this.bNewAcc.Text="New Account <<";
            }
            else
            {
                this.Width = 320;
                this.bNewAcc.Text = "New Account >>";
            }
        }

        private void bRegister_Click(object sender, EventArgs e)
        {
            if (this.tbRegLogin.Text != "" && this.tbRegPass.Text != "" &&
                this.tbRegPass.Text == this.tbRegConf.Text)
            {
                try
                {
                    this.chatClient.Init(this.cbServer.Text, Convert.ToInt32(this.nudPort.Value),
                        this.tbRegLogin.Text,
                        this.tbRegPass.Text);
                    this.chatClient.ValidateItselfAndServer();
                    this.chatClient.SetUpSecureChannel();
                    this.performReg();
                }
                catch (Exception)
                {
                    MessageBox.Show(@"Error registering user");
                }
            }
            else
            {
                MessageBox.Show("Invalid registration data");
            }
        }

        private void bCancel_Click(object sender, EventArgs e)
        { 
            this.Close(); 
        }

        private void performLogon()
        {
            int rs = chatClient.LogOn();
            switch (rs)
            {
                case 0://Success                    
                    chatClient.StartListener();
                    SelRoomForm selroomform1 = new SelRoomForm(chatClient);
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
            switch (chatClient.performRegDef(false))
            {
                case 0:
                    MessageBox.Show(String.Format("Registration success: User '{0}' is now registered", this.tbRegLogin.Text));
                    break;
                case 1:
                    MessageBox.Show(String.Format("Registration failed: User '{0}' already registered", this.tbRegLogin.Text));
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
