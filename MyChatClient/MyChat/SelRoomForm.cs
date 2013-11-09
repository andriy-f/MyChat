//using System.Linq;

namespace Andriy.MyChat.Client
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    public partial class SelRoomForm : Form
    {
        //volatile string[] rms = null;

        public SelRoomForm()
        {            
            this.InitializeComponent();            

            this.refreshRooms();
            this.tbRoom.Text = "r1";
            this.tbPass.Text="r1pass";
            this.tbConfPass.Text = "r1pass";
        }

        public void refreshRooms()
        {            
            this.cbRoomList.Text = "Refresh pending...";
            ChatClient.requestRooms(() =>
            {                
                string[] rms = ChatClient.getRooms();
                this.cbRoomListAction(rms);
            });            
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            if (ChatClient.msgProcessor.RoomCount == 0)
            {
                ChatClient.stopListener();
                Application.Exit();
            }
            else this.Close();
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            string room;
            if (this.rbNew.Checked)
            {
                room = this.tbRoom.Text;
            }
            else if (this.rbExisting.Checked)
            {
                room = this.cbRoomList.Text;
            }
            else throw new Exception("Radio buttons dont work");
            if (ChatClient.performJoinRoom(room, this.tbPass.Text))
            {
                ChatForm chatForm1 = new ChatForm(room);
                chatForm1.Show();
                this.Close();
            }
            else MessageBox.Show("Can't enter room:\n Probably invalid password");
        }

        private void rbNewExisting_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbNew.Checked)
            {
                this.tbRoom.Enabled = true;
                this.cbRoomList.Enabled = false;

                this.lConfPass.Visible = true;
                this.tbConfPass.Visible = true;
            }
            else if (this.rbExisting.Checked)
            {
                this.tbRoom.Enabled = false;
                this.cbRoomList.Enabled = true;

                this.lConfPass.Visible = false;
                this.tbConfPass.Visible = false;
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
            if (this.cbRoomList.InvokeRequired)
            {
                Action<string[]> act = this.cbRoomListAction;
                this.Invoke(act, new object[] { rooms });
            }
            else
            {                
                this.cbRoomList.Items.Clear();
                this.cbRoomList.Items.AddRange(rooms);

                if (rooms.Length > 0)
                    this.cbRoomList.SelectedIndex = 0;
                else
                    this.cbRoomList.Text = "<Empty>";
            }
        }
    }
}
