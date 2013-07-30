using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyChat
{
    public partial class ChatForm : Form
    {        
        public string room;

        public ChatForm()
        {
            InitializeComponent();            
        }

        public ChatForm(string lroom)
        {            
            InitializeComponent();
            room = lroom;
            this.Text = String.Format("User '{0}' at room '{1}' on server '{2}'", ChatClient.Login, room, ChatClient.Server);
            ChatClient.msgProcessor.addProcessor(room, OnReceiveMsg);//Ala event
            refreshDestination();
        }

        public void OnReceiveMsg(string source, string dest, string msg)
        {
            rtbHistory.AppendText(String.Format("[{0}]->[{1}]: \"{2}\"\n", source, dest, msg));
            rtbHistory.ScrollToCaret();
        }

        private void bSend_Click(object sender, EventArgs e)
        {
            if (cbDest.Text == "<Room>")            
                ChatClient.queueChatMsg(3, room, rtbMsg.Text);
            else if(cbDest.Text == "<All>")
                ChatClient.queueChatMsg(5, "All", rtbMsg.Text);
            else
                ChatClient.queueChatMsg(4, cbDest.Text, rtbMsg.Text);
            rtbMsg.Clear();
        }

        private void bLeave_Click(object sender, EventArgs e)
        {
            if (ChatClient.performLeaveRoom(room))            
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
            cbDest.Items.Clear();
            cbDest.Items.Add("<Room>");
            cbDest.Items.Add("<All>");
            cbDest.Text = "<Room>";
            ChatClient.requestRoomUsers(room, () =>
                {
                    string[] users = ChatClient.getRoomUsers();
                    refreshDestinationInvoke(users);
                });
        }

        private void refreshDestinationInvoke(string[] users)
        {
            if (this.cbDest.InvokeRequired)
            {
                Action<string[]> act = refreshDestinationInvoke;
                this.Invoke(act, new object[] { users });
            }
            else
            {
                cbDest.Items.Clear();
                cbDest.Items.Add("<Room>");
                cbDest.Items.Add("<All>");
                cbDest.Text = "<Room>";
                //Add users in room
                cbDest.Items.AddRange(users);
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
            refreshDestination();
        }
    }
}
