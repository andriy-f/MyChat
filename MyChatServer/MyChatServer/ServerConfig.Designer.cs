namespace Andriy.MyChat.Server
{
    partial class ServerConfig
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerConfig));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.loginsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.chatServerDataSet = new Andriy.MyChat.Server.ChatServerDataSet();
            this.loginsTableAdapter = new Andriy.MyChat.Server.ChatServerDataSetTableAdapters.LoginsTableAdapter();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpGeneral = new System.Windows.Forms.TabPage();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.tpLogins = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.loginsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.passwordsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bUpdate = new System.Windows.Forms.Button();
            this.bRefresh = new System.Windows.Forms.Button();
            this.tpConnection = new System.Windows.Forms.TabPage();
            this.tbInitCat = new System.Windows.Forms.TextBox();
            this.tbConfPass = new System.Windows.Forms.TextBox();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.tbUser = new System.Windows.Forms.TextBox();
            this.tbDataSource = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bApply = new System.Windows.Forms.Button();
            this.bExit = new System.Windows.Forms.Button();
            this.cmsChatServer = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLog = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.loginsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chatServerDataSet)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tpGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.tpLogins.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tpConnection.SuspendLayout();
            this.cmsChatServer.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.cmsChatServer;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "MyChat Server";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
            // 
            // loginsBindingSource
            // 
            this.loginsBindingSource.DataMember = "Logins";
            this.loginsBindingSource.DataSource = this.chatServerDataSet;
            // 
            // chatServerDataSet
            // 
            this.chatServerDataSet.DataSetName = "ChatServerDataSet";
            this.chatServerDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // loginsTableAdapter
            // 
            this.loginsTableAdapter.ClearBeforeFill = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tpGeneral);
            this.tabControl1.Controls.Add(this.tpLogins);
            this.tabControl1.Controls.Add(this.tpConnection);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(353, 407);
            this.tabControl1.TabIndex = 12;
            // 
            // tpGeneral
            // 
            this.tpGeneral.Controls.Add(this.nudPort);
            this.tpGeneral.Controls.Add(this.label1);
            this.tpGeneral.Location = new System.Drawing.Point(4, 22);
            this.tpGeneral.Name = "tpGeneral";
            this.tpGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tpGeneral.Size = new System.Drawing.Size(345, 381);
            this.tpGeneral.TabIndex = 0;
            this.tpGeneral.Text = "General";
            this.tpGeneral.UseVisualStyleBackColor = true;
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(40, 13);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(210, 20);
            this.nudPort.TabIndex = 12;
            this.nudPort.Value = new decimal(new int[] {
            6614,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Port";
            // 
            // tpLogins
            // 
            this.tpLogins.Controls.Add(this.dataGridView1);
            this.tpLogins.Controls.Add(this.bUpdate);
            this.tpLogins.Controls.Add(this.bRefresh);
            this.tpLogins.Location = new System.Drawing.Point(4, 22);
            this.tpLogins.Name = "tpLogins";
            this.tpLogins.Padding = new System.Windows.Forms.Padding(3);
            this.tpLogins.Size = new System.Drawing.Size(345, 381);
            this.tpLogins.TabIndex = 1;
            this.tpLogins.Text = "Logins";
            this.tpLogins.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.loginsDataGridViewTextBoxColumn,
            this.passwordsDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.loginsBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(345, 346);
            this.dataGridView1.TabIndex = 3;
            // 
            // loginsDataGridViewTextBoxColumn
            // 
            this.loginsDataGridViewTextBoxColumn.DataPropertyName = "Logins";
            this.loginsDataGridViewTextBoxColumn.HeaderText = "Logins";
            this.loginsDataGridViewTextBoxColumn.Name = "loginsDataGridViewTextBoxColumn";
            // 
            // passwordsDataGridViewTextBoxColumn
            // 
            this.passwordsDataGridViewTextBoxColumn.DataPropertyName = "Passwords";
            this.passwordsDataGridViewTextBoxColumn.HeaderText = "Passwords";
            this.passwordsDataGridViewTextBoxColumn.Name = "passwordsDataGridViewTextBoxColumn";
            // 
            // bUpdate
            // 
            this.bUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bUpdate.Location = new System.Drawing.Point(8, 352);
            this.bUpdate.Name = "bUpdate";
            this.bUpdate.Size = new System.Drawing.Size(75, 23);
            this.bUpdate.TabIndex = 4;
            this.bUpdate.Text = "Update";
            this.bUpdate.UseVisualStyleBackColor = true;
            this.bUpdate.Click += new System.EventHandler(this.bUpdate_Click);
            // 
            // bRefresh
            // 
            this.bRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bRefresh.Location = new System.Drawing.Point(89, 352);
            this.bRefresh.Name = "bRefresh";
            this.bRefresh.Size = new System.Drawing.Size(75, 23);
            this.bRefresh.TabIndex = 5;
            this.bRefresh.Text = "Refresh";
            this.bRefresh.UseVisualStyleBackColor = true;
            this.bRefresh.Click += new System.EventHandler(this.bRefresh_Click);
            // 
            // tpConnection
            // 
            this.tpConnection.Controls.Add(this.tbInitCat);
            this.tpConnection.Controls.Add(this.tbConfPass);
            this.tpConnection.Controls.Add(this.tbPass);
            this.tpConnection.Controls.Add(this.tbUser);
            this.tpConnection.Controls.Add(this.tbDataSource);
            this.tpConnection.Controls.Add(this.label6);
            this.tpConnection.Controls.Add(this.label5);
            this.tpConnection.Controls.Add(this.label4);
            this.tpConnection.Controls.Add(this.label3);
            this.tpConnection.Controls.Add(this.label2);
            this.tpConnection.Location = new System.Drawing.Point(4, 22);
            this.tpConnection.Name = "tpConnection";
            this.tpConnection.Size = new System.Drawing.Size(345, 381);
            this.tpConnection.TabIndex = 2;
            this.tpConnection.Text = "DB Connection";
            this.tpConnection.UseVisualStyleBackColor = true;
            // 
            // tbInitCat
            // 
            this.tbInitCat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbInitCat.Location = new System.Drawing.Point(116, 123);
            this.tbInitCat.Name = "tbInitCat";
            this.tbInitCat.Size = new System.Drawing.Size(206, 20);
            this.tbInitCat.TabIndex = 9;
            // 
            // tbConfPass
            // 
            this.tbConfPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbConfPass.Location = new System.Drawing.Point(116, 90);
            this.tbConfPass.Name = "tbConfPass";
            this.tbConfPass.Size = new System.Drawing.Size(206, 20);
            this.tbConfPass.TabIndex = 8;
            this.tbConfPass.UseSystemPasswordChar = true;
            // 
            // tbPass
            // 
            this.tbPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbPass.Location = new System.Drawing.Point(116, 68);
            this.tbPass.Name = "tbPass";
            this.tbPass.Size = new System.Drawing.Size(206, 20);
            this.tbPass.TabIndex = 7;
            this.tbPass.UseSystemPasswordChar = true;
            // 
            // tbUser
            // 
            this.tbUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbUser.Location = new System.Drawing.Point(116, 37);
            this.tbUser.Name = "tbUser";
            this.tbUser.Size = new System.Drawing.Size(206, 20);
            this.tbUser.TabIndex = 6;
            // 
            // tbDataSource
            // 
            this.tbDataSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDataSource.Location = new System.Drawing.Point(116, 7);
            this.tbDataSource.Name = "tbDataSource";
            this.tbDataSource.Size = new System.Drawing.Size(206, 20);
            this.tbDataSource.TabIndex = 5;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 126);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Initial Catalog";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 93);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Confirm Password";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Password";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "User";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Data Source";
            // 
            // bApply
            // 
            this.bApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bApply.Location = new System.Drawing.Point(185, 412);
            this.bApply.Name = "bApply";
            this.bApply.Size = new System.Drawing.Size(75, 23);
            this.bApply.TabIndex = 15;
            this.bApply.Text = "Apply";
            this.bApply.UseVisualStyleBackColor = true;
            this.bApply.Click += new System.EventHandler(this.bApply_Click);
            // 
            // bExit
            // 
            this.bExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bExit.Location = new System.Drawing.Point(266, 412);
            this.bExit.Name = "bExit";
            this.bExit.Size = new System.Drawing.Size(75, 23);
            this.bExit.TabIndex = 14;
            this.bExit.Text = "Exit";
            this.bExit.UseVisualStyleBackColor = true;
            this.bExit.Click += new System.EventHandler(this.bExit_Click);
            // 
            // cmsChatServer
            // 
            this.cmsChatServer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiConfig,
            this.tsmiLog,
            this.tsmiExit});
            this.cmsChatServer.Name = "cmsChatServer";
            this.cmsChatServer.Size = new System.Drawing.Size(153, 92);
            // 
            // tsmiConfig
            // 
            this.tsmiConfig.Name = "tsmiConfig";
            this.tsmiConfig.Size = new System.Drawing.Size(152, 22);
            this.tsmiConfig.Text = "Config";
            this.tsmiConfig.Click += new System.EventHandler(this.tsmiConfig_Click);
            // 
            // tsmiLog
            // 
            this.tsmiLog.Name = "tsmiLog";
            this.tsmiLog.Size = new System.Drawing.Size(152, 22);
            this.tsmiLog.Text = "Log";
            this.tsmiLog.Click += new System.EventHandler(this.tsmiLog_Click);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.Size = new System.Drawing.Size(152, 22);
            this.tsmiExit.Text = "Exit";
            this.tsmiExit.Click += new System.EventHandler(this.tsmiExit_Click);
            // 
            // ServerConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(353, 447);
            this.Controls.Add(this.bApply);
            this.Controls.Add(this.bExit);
            this.Controls.Add(this.tabControl1);
            this.Name = "ServerConfig";
            this.Text = "Chat Server Configuration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServerConfig_FormClosing);
            this.Load += new System.EventHandler(this.ServerConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.loginsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chatServerDataSet)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tpGeneral.ResumeLayout(false);
            this.tpGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.tpLogins.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tpConnection.ResumeLayout(false);
            this.tpConnection.PerformLayout();
            this.cmsChatServer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private ChatServerDataSet chatServerDataSet;
        private System.Windows.Forms.BindingSource loginsBindingSource;
        private Andriy.MyChat.Server.ChatServerDataSetTableAdapters.LoginsTableAdapter loginsTableAdapter;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpGeneral;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tpLogins;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn loginsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn passwordsDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button bUpdate;
        private System.Windows.Forms.Button bRefresh;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Button bExit;
        private System.Windows.Forms.TabPage tpConnection;
        private System.Windows.Forms.TextBox tbInitCat;
        private System.Windows.Forms.TextBox tbConfPass;
        private System.Windows.Forms.TextBox tbPass;
        private System.Windows.Forms.TextBox tbUser;
        private System.Windows.Forms.TextBox tbDataSource;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ContextMenuStrip cmsChatServer;
        private System.Windows.Forms.ToolStripMenuItem tsmiConfig;
        private System.Windows.Forms.ToolStripMenuItem tsmiLog;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
    }
}

