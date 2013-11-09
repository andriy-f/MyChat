//using System.Linq;

namespace Andriy.MyChat.Client
{
    using System;
    using System.Windows.Forms;

    public partial class ChatForm : Form
    {        
        public string room;

        public ChatForm()
        {
            this.InitializeComponent();            
        }

        public ChatForm(string lroom)
        {            
            this.InitializeComponent();
            this.room = lroom;
            this.Text = String.Format("User '{0}' at room '{1}' on server '{2}'", ChatClient.Login, this.room, ChatClient.Server);
            ChatClient.msgProcessor.addProcessor(this.room, this.OnReceiveMsg);//Ala event
            this.refreshDestination();
        }

        public void OnReceiveMsg(string source, string dest, string msg)
        {
            this.rtbHistory.AppendText(String.Format("[{0}]->[{1}]: \"{2}\"\n", source, dest, msg));
            this.rtbHistory.ScrollToCaret();
        }

        private void bSend_Click(object sender, EventArgs e)
        {
            if (this.cbDest.Text == "<Room>")            
                ChatClient.queueChatMsg(3, this.room, this.rtbMsg.Text);
            else if(this.cbDest.Text == "<All>")
                ChatClient.queueChatMsg(5, "All", this.rtbMsg.Text);
            else
                ChatClient.queueChatMsg(4, this.cbDest.Text, this.rtbMsg.Text);
            this.rtbMsg.Clear();
        }

        private void bLeave_Click(object sender, EventArgs e)
        {
            if (ChatClient.performLeaveRoom(this.room))            
                this.Close();
            else MessageBox.Show("Error while leaving room");
        }

        private void bJoinAnRoom_Click(object sender, EventArgs e)
        {
            SelRoomForm selroomform1 = new SelRoomForm();
            selroomform1.Show();
        }        

        private void refreshDestination()
        {
            this.cbDest.Items.Clear();
            this.cbDest.Items.Add("<Room>");
            this.cbDest.Items.Add("<All>");
            this.cbDest.Text = "<Room>";
            ChatClient.requestRoomUsers(this.room, () =>
                {
                    string[] users = ChatClient.getRoomUsers();
                    this.refreshDestinationInvoke(users);
                });
        }

        private void refreshDestinationInvoke(string[] users)
        {
            if (this.cbDest.InvokeRequired)
            {
                Action<string[]> act = this.refreshDestinationInvoke;
                this.Invoke(act, new object[] { users });
            }
            else
            {
                this.cbDest.Items.Clear();
                this.cbDest.Items.Add("<Room>");
                this.cbDest.Items.Add("<All>");
                this.cbDest.Text = "<Room>";
                //Add users in room
                this.cbDest.Items.AddRange(users);
            }
        }

        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ChatClient.stopListener();
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void cbDest_DropDown(object sender, EventArgs e)
        {
            this.refreshDestination();
        }
    }
}
