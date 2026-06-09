namespace RemoteControl.Server
{
    partial class FrmHVNC
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonStart = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSplitButtonCapture = new System.Windows.Forms.ToolStripSplitButton();
            this.toolStripMenuItemCaptureMouse = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemCaptureKeyboard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSplitButtonFPS = new System.Windows.Forms.ToolStripSplitButton();
            this.toolStripMenuItemFPS1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemFPS3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemFPS5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemFPS10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRunProcess = new System.Windows.Forms.ToolStripButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonStart,
            this.toolStripButtonStop,
            this.toolStripSeparator1,
            this.toolStripSplitButtonCapture,
            this.toolStripSeparator2,
            this.toolStripSplitButtonFPS,
            this.toolStripSeparator3,
            this.toolStripButtonRunProcess});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(900, 25);
            this.toolStrip1.TabIndex = 0;
            // 
            // toolStripButtonStart
            // 
            this.toolStripButtonStart.Name = "toolStripButtonStart";
            this.toolStripButtonStart.Size = new System.Drawing.Size(72, 22);
            this.toolStripButtonStart.Text = "启动HVNC";
            this.toolStripButtonStart.Click += new System.EventHandler(this.toolStripButtonStart_Click);
            // 
            // toolStripButtonStop
            // 
            this.toolStripButtonStop.Enabled = false;
            this.toolStripButtonStop.Name = "toolStripButtonStop";
            this.toolStripButtonStop.Size = new System.Drawing.Size(72, 22);
            this.toolStripButtonStop.Text = "停止HVNC";
            this.toolStripButtonStop.Click += new System.EventHandler(this.toolStripButtonStop_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripSplitButtonCapture
            // 
            this.toolStripSplitButtonCapture.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripSplitButtonCapture.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemCaptureMouse,
            this.toolStripMenuItemCaptureKeyboard});
            this.toolStripSplitButtonCapture.Name = "toolStripSplitButtonCapture";
            this.toolStripSplitButtonCapture.Size = new System.Drawing.Size(72, 22);
            this.toolStripSplitButtonCapture.Text = "捕捉操作";
            this.toolStripSplitButtonCapture.ButtonClick += new System.EventHandler(this.toolStripSplitButtonCapture_ButtonClick);
            // 
            // toolStripMenuItemCaptureMouse
            // 
            this.toolStripMenuItemCaptureMouse.Name = "toolStripMenuItemCaptureMouse";
            this.toolStripMenuItemCaptureMouse.Size = new System.Drawing.Size(100, 22);
            this.toolStripMenuItemCaptureMouse.Text = "鼠标";
            this.toolStripMenuItemCaptureMouse.Click += new System.EventHandler(this.toolStripMenuItemCaptureMouse_Click);
            // 
            // toolStripMenuItemCaptureKeyboard
            // 
            this.toolStripMenuItemCaptureKeyboard.Name = "toolStripMenuItemCaptureKeyboard";
            this.toolStripMenuItemCaptureKeyboard.Size = new System.Drawing.Size(100, 22);
            this.toolStripMenuItemCaptureKeyboard.Text = "键盘";
            this.toolStripMenuItemCaptureKeyboard.Click += new System.EventHandler(this.toolStripMenuItemCaptureKeyboard_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripSplitButtonFPS
            // 
            this.toolStripSplitButtonFPS.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripSplitButtonFPS.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemFPS1,
            this.toolStripMenuItemFPS3,
            this.toolStripMenuItemFPS5,
            this.toolStripMenuItemFPS10});
            this.toolStripSplitButtonFPS.Name = "toolStripSplitButtonFPS";
            this.toolStripSplitButtonFPS.Size = new System.Drawing.Size(72, 22);
            this.toolStripSplitButtonFPS.Text = "帧率选择";
            this.toolStripSplitButtonFPS.ButtonClick += new System.EventHandler(this.toolStripSplitButtonFPS_ButtonClick);
            // 
            // toolStripMenuItemFPS1
            // 
            this.toolStripMenuItemFPS1.Name = "toolStripMenuItemFPS1";
            this.toolStripMenuItemFPS1.Size = new System.Drawing.Size(90, 22);
            this.toolStripMenuItemFPS1.Tag = "1";
            this.toolStripMenuItemFPS1.Text = "1";
            this.toolStripMenuItemFPS1.Click += new System.EventHandler(this.toolStripMenuItemFPS_Click);
            // 
            // toolStripMenuItemFPS3
            // 
            this.toolStripMenuItemFPS3.Name = "toolStripMenuItemFPS3";
            this.toolStripMenuItemFPS3.Size = new System.Drawing.Size(90, 22);
            this.toolStripMenuItemFPS3.Tag = "3";
            this.toolStripMenuItemFPS3.Text = "3";
            this.toolStripMenuItemFPS3.Click += new System.EventHandler(this.toolStripMenuItemFPS_Click);
            // 
            // toolStripMenuItemFPS5
            // 
            this.toolStripMenuItemFPS5.Checked = true;
            this.toolStripMenuItemFPS5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripMenuItemFPS5.Name = "toolStripMenuItemFPS5";
            this.toolStripMenuItemFPS5.Size = new System.Drawing.Size(90, 22);
            this.toolStripMenuItemFPS5.Tag = "5";
            this.toolStripMenuItemFPS5.Text = "5";
            this.toolStripMenuItemFPS5.Click += new System.EventHandler(this.toolStripMenuItemFPS_Click);
            // 
            // toolStripMenuItemFPS10
            // 
            this.toolStripMenuItemFPS10.Name = "toolStripMenuItemFPS10";
            this.toolStripMenuItemFPS10.Size = new System.Drawing.Size(90, 22);
            this.toolStripMenuItemFPS10.Tag = "10";
            this.toolStripMenuItemFPS10.Text = "10";
            this.toolStripMenuItemFPS10.Click += new System.EventHandler(this.toolStripMenuItemFPS_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRunProcess
            // 
            this.toolStripButtonRunProcess.Name = "toolStripButtonRunProcess";
            this.toolStripButtonRunProcess.Size = new System.Drawing.Size(60, 22);
            this.toolStripButtonRunProcess.Text = "启动程序";
            this.toolStripButtonRunProcess.Click += new System.EventHandler(this.toolStripButtonRunProcess_Click);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 25);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(900, 575);
            this.panel1.TabIndex = 1;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 50);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDoubleClick);
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // FrmHVNC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.KeyPreview = true;
            this.Name = "FrmHVNC";
            this.Text = "HVNC \u9690\u5f62\u684c\u9762";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmHVNC_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmHVNC_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FrmHVNC_KeyUp);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonStart;
        private System.Windows.Forms.ToolStripButton toolStripButtonStop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButtonCapture;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemCaptureMouse;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemCaptureKeyboard;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButtonFPS;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFPS1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFPS3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFPS5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFPS10;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripButtonRunProcess;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
