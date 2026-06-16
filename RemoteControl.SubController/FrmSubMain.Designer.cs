using System.Drawing;
using System.Windows.Forms;

namespace RemoteControl.SubController
{
    partial class FrmSubMain
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelTop;
        private Label labelTitle;
        private Label labelStatus;
        private Label labelClientCount;
        private Button btnConnect;
        private Button btnSettings;
        private ListView hostListView;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new Panel();
            this.labelTitle = new Label();
            this.labelStatus = new Label();
            this.labelClientCount = new Label();
            this.btnConnect = new Button();
            this.btnSettings = new Button();
            this.hostListView = new ListView();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();

            // panelTop
            this.panelTop.BackColor = Color.FromArgb(45, 45, 48);
            this.panelTop.Dock = DockStyle.Top;
            this.panelTop.Height = 44;
            this.panelTop.Controls.Add(this.btnSettings);
            this.panelTop.Controls.Add(this.btnConnect);
            this.panelTop.Controls.Add(this.labelClientCount);
            this.panelTop.Controls.Add(this.labelStatus);
            this.panelTop.Controls.Add(this.labelTitle);

            // labelTitle
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            this.labelTitle.ForeColor = Color.White;
            this.labelTitle.Location = new Point(12, 10);
            this.labelTitle.Text = "副控管理端";

            // labelStatus
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new Font("Microsoft YaHei", 9F);
            this.labelStatus.ForeColor = Color.OrangeRed;
            this.labelStatus.Location = new Point(130, 14);
            this.labelStatus.Text = "未连接";

            // labelClientCount
            this.labelClientCount.AutoSize = true;
            this.labelClientCount.Font = new Font("Microsoft YaHei", 9F);
            this.labelClientCount.ForeColor = Color.LightGray;
            this.labelClientCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.labelClientCount.Location = new Point(600, 14);
            this.labelClientCount.Text = "在线: 0";

            // btnConnect
            this.btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnConnect.FlatStyle = FlatStyle.Flat;
            this.btnConnect.ForeColor = Color.White;
            this.btnConnect.Font = new Font("Microsoft YaHei", 9F);
            this.btnConnect.Location = new Point(700, 8);
            this.btnConnect.Size = new Size(70, 28);
            this.btnConnect.Text = "连接";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

            // btnSettings
            this.btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnSettings.FlatStyle = FlatStyle.Flat;
            this.btnSettings.ForeColor = Color.White;
            this.btnSettings.Font = new Font("Microsoft YaHei", 9F);
            this.btnSettings.Location = new Point(780, 8);
            this.btnSettings.Size = new Size(70, 28);
            this.btnSettings.Text = "设置";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            // hostListView
            this.hostListView.Dock = DockStyle.Fill;
            this.hostListView.View = View.Details;
            this.hostListView.FullRowSelect = true;
            this.hostListView.GridLines = true;
            this.hostListView.HideSelection = false;
            this.hostListView.MultiSelect = true;
            this.hostListView.BackColor = Color.White;
            this.hostListView.Font = new Font("Microsoft YaHei", 9F);
            this.hostListView.BorderStyle = BorderStyle.FixedSingle;

            // FrmSubMain
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 600);
            this.Controls.Add(this.hostListView);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new Size(750, 450);
            this.Name = "FrmSubMain";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "副控管理端";
            this.Load += new System.EventHandler(this.FrmSubMain_Load);
            this.FormClosing += new FormClosingEventHandler(this.FrmSubMain_FormClosing);

            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
