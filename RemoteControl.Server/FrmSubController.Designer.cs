namespace RemoteControl.Server
{
    partial class FrmSubController
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.treeViewClients = new System.Windows.Forms.TreeView();
            this.splitContainerRight = new System.Windows.Forms.SplitContainer();
            this.listViewInfo = new System.Windows.Forms.ListView();
            this.colProperty = new System.Windows.Forms.ColumnHeader();
            this.colValue = new System.Windows.Forms.ColumnHeader();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTitle = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();

            this.splitContainerMain.BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.splitContainerRight.BeginInit();
            this.splitContainerRight.Panel1.SuspendLayout();
            this.splitContainerRight.Panel2.SuspendLayout();
            this.splitContainerRight.SuspendLayout();
            this.panelTitle.SuspendLayout();
            this.SuspendLayout();

            // splitContainerMain
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 36);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.SplitterDistance = 220;
            this.splitContainerMain.TabIndex = 0;

            // treeViewClients
            this.treeViewClients.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewClients.FullRowSelect = true;
            this.treeViewClients.HideSelection = false;
            this.treeViewClients.Name = "treeViewClients";
            this.treeViewClients.ShowRootLines = false;
            this.treeViewClients.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewClients_AfterSelect);

            // splitContainerRight
            this.splitContainerRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainerRight.SplitterDistance = 260;
            this.splitContainerRight.TabIndex = 0;

            // listViewInfo
            this.listViewInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colProperty, this.colValue });
            this.listViewInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewInfo.FullRowSelect = true;
            this.listViewInfo.GridLines = true;
            this.listViewInfo.Name = "listViewInfo";
            this.listViewInfo.View = System.Windows.Forms.View.Details;

            // colProperty
            this.colProperty.Text = "属性";
            this.colProperty.Width = 120;

            // colValue
            this.colValue.Text = "值";
            this.colValue.Width = 400;

            // textBoxLog
            this.textBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.BackColor = System.Drawing.Color.White;

            // contextMenu
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.toolStripMenuItem1,
                this.toolStripMenuItem2,
                this.toolStripMenuItem3,
                this.toolStripMenuItem4,
                this.toolStripMenuItem5,
                this.toolStripSeparator1,
                this.toolStripMenuItem6,
                this.toolStripMenuItem7,
                this.toolStripMenuItem8 });
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(149, 170);

            // menuItem1 - 文件管理
            this.toolStripMenuItem1.Text = "文件管理(&F)";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.onSubMenuFileManager);

            // menuItem2 - 屏幕监控
            this.toolStripMenuItem2.Text = "屏幕监控(&S)";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.onSubMenuScreenCapture);

            // menuItem3 - 高清屏幕
            this.toolStripMenuItem3.Text = "高清屏幕(&H)";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.onSubMenuHDScreen);

            // menuItem4 - 系统管理
            this.toolStripMenuItem4.Text = "系统管理(&M)";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.onSubMenuSystemManager);

            // menuItem5 - 视频查看
            this.toolStripMenuItem5.Text = "视频查看(&V)";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.onSubMenuVideoCapture);

            // toolStripSeparator1
            this.toolStripSeparator1.Name = "toolStripSeparator1";

            // menuItem6 - 编辑备注
            this.toolStripMenuItem6.Text = "编辑备注(&R)";
            this.toolStripMenuItem6.Click += new System.EventHandler(this.onSubMenuChangeRemark);

            // menuItem7 - 创建分组
            this.toolStripMenuItem7.Text = "创建分组(&G)";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.onSubMenuCreateGroup);

            // menuItem8 - 会话详情
            this.toolStripMenuItem8.Text = "会话详情(&I)";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.onSubMenuSessionInfo);

            // panelTitle
            this.panelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitle.Height = 36;
            this.panelTitle.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            // labelTitle
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(12, 8);
            this.labelTitle.Text = "副控面板";

            // labelStatus
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.labelStatus.ForeColor = System.Drawing.Color.LightGray;
            this.labelStatus.Location = new System.Drawing.Point(100, 10);
            this.labelStatus.Text = "在线: 0";

            // splitContainerMain panels
            this.splitContainerMain.Panel1.Controls.Add(this.treeViewClients);
            this.splitContainerMain.Panel2.Controls.Add(this.splitContainerRight);

            // splitContainerRight panels
            this.splitContainerRight.Panel1.Controls.Add(this.listViewInfo);
            this.splitContainerRight.Panel2.Controls.Add(this.textBoxLog);

            // FrmSubController
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 550);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelTitle);
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "FrmSubController";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "副控面板";
            this.Load += new System.EventHandler(this.FrmSubController_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmSubController_FormClosing);

            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerRight.Panel1.ResumeLayout(false);
            this.splitContainerRight.Panel2.ResumeLayout(false);
            this.splitContainerRight.Panel2.PerformLayout();
            this.splitContainerRight.ResumeLayout(false);
            this.panelTitle.ResumeLayout(false);
            this.panelTitle.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TreeView treeViewClients;
        private System.Windows.Forms.SplitContainer splitContainerRight;
        private System.Windows.Forms.ListView listViewInfo;
        private System.Windows.Forms.ColumnHeader colProperty;
        private System.Windows.Forms.ColumnHeader colValue;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.Panel panelTitle;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelStatus;
    }
}
