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
    public partial class SelRoomForm : Form
    {
        //volatile string[] rms = null;

        public SelRoomForm()
        {            
            InitializeComponent();            

            refreshRooms();
            tbRoom.Text = "r1";
            tbPass.Text="r1pass";
            tbConfPass.Text = "r1pass";
        }

        public void refreshRooms()
        {            
            cbRoomList.Text = "Refresh pending...";
            ChatClient.requestRooms(() =>
            {                
                string[] rms = ChatClient.getRooms();
                cbRoomListAction(rms);
            });            
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            if (ChatClient.msgProcessor.RoomCount == 0)
            {
                ChatClient.stopListener();
                Application.Exit();
            }
            else Close();
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            string room;
            if (rbNew.Checked)
            {
                room = tbRoom.Text;
            }
            else if (rbExisting.Checked)
            {
                room = cbRoomList.Text;
            }
            else throw new Exception("Radio buttons dont work");
            if (ChatClient.performJoinRoom(room, tbPass.Text))
            {
                ChatForm chatForm1 = new ChatForm(room);
                chatForm1.Show();
                Close();
            }
            else MessageBox.Show("Can't enter room:\n Probably invalid password");
        }

        private void rbNewExisting_CheckedChanged(object sender, EventArgs e)
        {
            if (rbNew.Checked)
            {
                tbRoom.Enabled = true;
                cbRoomList.Enabled = false;

                lConfPass.Visible = true;
                tbConfPass.Visible = true;
            }
            else if (rbExisting.Checked)
            {
                tbRoom.Enabled = false;
                cbRoomList.Enabled = true;

                lConfPass.Visible = false;
                tbConfPass.Visible = false;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private void cbRoomListAction(string[] rooms)
        {
            if (cbRoomList.InvokeRequired)
            {
                Action<string[]> act = cbRoomListAction;
                this.Invoke(act, new object[] { rooms });
            }
            else
            {                
                cbRoomList.Items.Clear();
                cbRoomList.Items.AddRange(rooms);

                if (rooms.Length > 0)
                    cbRoomList.SelectedIndex = 0;
                else
                    cbRoomList.Text = "<Empty>";
            }
        }
    }
}
