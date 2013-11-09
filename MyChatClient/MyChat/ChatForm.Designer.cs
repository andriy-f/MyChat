namespace Andriy.MyChat.Client
{
    partial class ChatForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbHistory = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.bJoinAnRoom = new System.Windows.Forms.Button();
            this.bLeave = new System.Windows.Forms.Button();
            this.bSend = new System.Windows.Forms.Button();
            this.rtbMsg = new System.Windows.Forms.RichTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.cbDest = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtbHistory
            // 
            this.rtbHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbHistory.Location = new System.Drawing.Point(0, 0);
            this.rtbHistory.Name = "rtbHistory";
            this.rtbHistory.ReadOnly = true;
            this.rtbHistory.Size = new System.Drawing.Size(540, 248);
            this.rtbHistory.TabIndex = 0;
            this.rtbHistory.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.bJoinAnRoom);
            this.panel1.Controls.Add(this.bLeave);
            this.panel1.Controls.Add(this.bSend);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 353);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(540, 44);
            this.panel1.TabIndex = 2;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(237, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Test";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Visible = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // bJoinAnRoom
            // 
            this.bJoinAnRoom.Location = new System.Drawing.Point(333, 12);
            this.bJoinAnRoom.Name = "bJoinAnRoom";
            this.bJoinAnRoom.Size = new System.Drawing.Size(102, 23);
            this.bJoinAnRoom.TabIndex = 0;
            this.bJoinAnRoom.Text = "Join another room";
            this.bJoinAnRoom.UseVisualStyleBackColor = true;
            this.bJoinAnRoom.Click += new System.EventHandler(this.bJoinAnRoom_Click);
            // 
            // bLeave
            // 
            this.bLeave.Location = new System.Drawing.Point(441, 12);
            this.bLeave.Name = "bLeave";
            this.bLeave.Size = new System.Drawing.Size(75, 23);
            this.bLeave.TabIndex = 1;
            this.bLeave.Text = "Leave";
            this.bLeave.UseVisualStyleBackColor = true;
            this.bLeave.Click += new System.EventHandler(this.bLeave_Click);
            // 
            // bSend
            // 
            this.bSend.Location = new System.Drawing.Point(4, 7);
            this.bSend.Name = "bSend";
            this.bSend.Size = new System.Drawing.Size(75, 23);
            this.bSend.TabIndex = 0;
            this.bSend.Text = "Send";
            this.bSend.UseVisualStyleBackColor = true;
            this.bSend.Click += new System.EventHandler(this.bSend_Click);
            // 
            // rtbMsg
            // 
            this.rtbMsg.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rtbMsg.Location = new System.Drawing.Point(0, 285);
            this.rtbMsg.Name = "rtbMsg";
            this.rtbMsg.Size = new System.Drawing.Size(540, 68);
            this.rtbMsg.TabIndex = 3;
            this.rtbMsg.Text = "";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.cbDest);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 246);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(540, 39);
            this.panel2.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Receiver:";
            // 
            // cbDest
            // 
            this.cbDest.FormattingEnabled = true;
            this.cbDest.Location = new System.Drawing.Point(81, 8);
            this.cbDest.Name = "cbDest";
            this.cbDest.Size = new System.Drawing.Size(121, 21);
            this.cbDest.TabIndex = 0;
            this.cbDest.DropDown += new System.EventHandler(this.cbDest_DropDown);
            // 
            // ChatForm
            // 
            this.AcceptButton = this.bSend;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 397);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.rtbMsg);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.rtbHistory);
            this.Name = "ChatForm";
            this.Text = "ChatForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ChatForm_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbHistory;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox rtbMsg;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button bSend;
        private System.Windows.Forms.Button bJoinAnRoom;
        private System.Windows.Forms.Button bLeave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbDest;
        private System.Windows.Forms.Button button1;
    }
}