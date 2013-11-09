namespace Andriy.MyChat.Client
{
    partial class SelRoomForm
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
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.cbRoomList = new System.Windows.Forms.ComboBox();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbRoom = new System.Windows.Forms.TextBox();
            this.lConfPass = new System.Windows.Forms.Label();
            this.tbConfPass = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbExisting = new System.Windows.Forms.RadioButton();
            this.rbNew = new System.Windows.Forms.RadioButton();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bOK
            // 
            this.bOK.Location = new System.Drawing.Point(135, 170);
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size(80, 23);
            this.bOK.TabIndex = 0;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            this.bOK.Click += new System.EventHandler(this.bOK_Click);
            // 
            // bCancel
            // 
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point(221, 170);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(80, 23);
            this.bCancel.TabIndex = 1;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // cbRoomList
            // 
            this.cbRoomList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append;
            this.cbRoomList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbRoomList.Enabled = false;
            this.cbRoomList.FormattingEnabled = true;
            this.cbRoomList.Items.AddRange(new object[] {
            "Room1",
            "Room2"});
            this.cbRoomList.Location = new System.Drawing.Point(73, 42);
            this.cbRoomList.Name = "cbRoomList";
            this.cbRoomList.Size = new System.Drawing.Size(210, 21);
            this.cbRoomList.TabIndex = 2;
            // 
            // tbPass
            // 
            this.tbPass.Location = new System.Drawing.Point(115, 100);
            this.tbPass.Name = "tbPass";
            this.tbPass.Size = new System.Drawing.Size(186, 20);
            this.tbPass.TabIndex = 3;
            this.tbPass.UseSystemPasswordChar = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 103);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Password:";
            // 
            // tbRoom
            // 
            this.tbRoom.Location = new System.Drawing.Point(73, 19);
            this.tbRoom.Name = "tbRoom";
            this.tbRoom.Size = new System.Drawing.Size(210, 20);
            this.tbRoom.TabIndex = 8;
            // 
            // lConfPass
            // 
            this.lConfPass.AutoSize = true;
            this.lConfPass.Location = new System.Drawing.Point(15, 129);
            this.lConfPass.Name = "lConfPass";
            this.lConfPass.Size = new System.Drawing.Size(94, 13);
            this.lConfPass.TabIndex = 12;
            this.lConfPass.Text = "Confirm Password:";
            // 
            // tbConfPass
            // 
            this.tbConfPass.Location = new System.Drawing.Point(115, 126);
            this.tbConfPass.Name = "tbConfPass";
            this.tbConfPass.Size = new System.Drawing.Size(186, 20);
            this.tbConfPass.TabIndex = 11;
            this.tbConfPass.UseSystemPasswordChar = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbExisting);
            this.groupBox1.Controls.Add(this.rbNew);
            this.groupBox1.Controls.Add(this.cbRoomList);
            this.groupBox1.Controls.Add(this.tbRoom);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(289, 72);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Room";
            // 
            // rbExisting
            // 
            this.rbExisting.AutoSize = true;
            this.rbExisting.Location = new System.Drawing.Point(6, 43);
            this.rbExisting.Name = "rbExisting";
            this.rbExisting.Size = new System.Drawing.Size(61, 17);
            this.rbExisting.TabIndex = 1;
            this.rbExisting.Text = "Existing";
            this.rbExisting.UseVisualStyleBackColor = true;
            this.rbExisting.CheckedChanged += new System.EventHandler(this.rbNewExisting_CheckedChanged);
            // 
            // rbNew
            // 
            this.rbNew.AutoSize = true;
            this.rbNew.Checked = true;
            this.rbNew.Location = new System.Drawing.Point(6, 20);
            this.rbNew.Name = "rbNew";
            this.rbNew.Size = new System.Drawing.Size(47, 17);
            this.rbNew.TabIndex = 0;
            this.rbNew.TabStop = true;
            this.rbNew.Text = "New";
            this.rbNew.UseVisualStyleBackColor = true;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // SelRoomForm
            // 
            this.AcceptButton = this.bOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size(314, 209);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lConfPass);
            this.Controls.Add(this.tbConfPass);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbPass);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "SelRoomForm";
            this.Text = "Select Room";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.ComboBox cbRoomList;
        private System.Windows.Forms.TextBox tbPass;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbRoom;
        private System.Windows.Forms.Label lConfPass;
        private System.Windows.Forms.TextBox tbConfPass;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbExisting;
        private System.Windows.Forms.RadioButton rbNew;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}