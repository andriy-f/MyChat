//using System.Linq;

namespace Andriy.MyChat.Client
{
    using System;
    using System.Windows.Forms;

    public partial class ChatForm : Form
    {
        private ChatClient chatClient;
        
        public string room;

        public ChatForm(ChatClient chatClient, string lroom)
        {            
            this.InitializeComponent();

            this.chatClient = chatClient;
            this.room = lroom;
            this.Text = String.Format("User '{0}' at room '{1}' on server '{2}'", chatClient.Login, this.room, chatClient.Server);
            chatClient.msgProcessor.addProcessor(this.room, this.OnReceiveMsg);//Ala event
            this.refreshDestination();
        }

        private void OnReceiveMsg(string source, string dest, string msg)
        {
            if (this.rtbHistory.InvokeRequired)
            {
                var callback = new Action<string, string, string>(this.OnReceiveMsg);
                this.Invoke(callback, new object[] { source, dest, msg });
            }
            else
            {
                this.rtbHistory.AppendText(String.Format("[{0}]->[{1}]: \"{2}\"\n", source, dest, msg));
                this.rtbHistory.ScrollToCaret();
            }
        }

        private void bSend_Click(object sender, EventArgs e)
        {
            if (this.cbDest.Text == "<Room>")
                chatClient.queueChatMsg(3, this.room, this.rtbMsg.Text);
            else if(this.cbDest.Text == "<All>")
                chatClient.queueChatMsg(5, "All", this.rtbMsg.Text);
            else
                chatClient.queueChatMsg(4, this.cbDest.Text, this.rtbMsg.Text);
            this.rtbMsg.Clear();
        }

        private void bLeave_Click(object sender, EventArgs e)
        {
            if (chatClient.performLeaveRoom(this.room))            
                this.Close();
            else MessageBox.Show("Error while leaving room");
        }

        private void bJoinAnRoom_Click(object sender, EventArgs e)
        {
            SelRoomForm selroomform1 = new SelRoomForm(chatClient);
            selroomform1.Show();
        }        

        private void refreshDestination()
        {
            this.cbDest.Items.Clear();
            this.cbDest.Items.Add("<Room>");
            this.cbDest.Items.Add("<All>");
            this.cbDest.Text = "<Room>";
            chatClient.requestRoomUsers(this.room, () =>
                {
                    string[] users = chatClient.getRoomUsers();
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
            chatClient.stopListener();
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
