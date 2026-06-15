using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmHostInfo : FrmBase
    {
        private SocketSession _session;
        private TabControl _tabControl;
        private Label _statusLabel;

        public FrmHostInfo(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "主机信息 — " + hostName;
            this.Size = new Size(520, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            InitializeControls();
            RefreshInfo();
        }

        private void InitializeControls()
        {
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Font = new Font("微软雅黑", 9F);

            var tabDisk = new TabPage("磁盘");
            var tabNet = new TabPage("网络");
            var tabSoft = new TabPage("软件安装");
            var tabSec = new TabPage("安全软件");

            _tabControl.TabPages.Add(tabDisk);
            _tabControl.TabPages.Add(tabNet);
            _tabControl.TabPages.Add(tabSoft);
            _tabControl.TabPages.Add(tabSec);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 28;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Font = new Font("微软雅黑", 9F);

            var refreshBtn = new Button();
            refreshBtn.Text = "刷新(R)";
            refreshBtn.Dock = DockStyle.Bottom;
            refreshBtn.Height = 28;
            refreshBtn.Click += (s, e) => RefreshInfo();

            this.Controls.Add(_tabControl);
            this.Controls.Add(refreshBtn);
            this.Controls.Add(_statusLabel);
        }

        private void RefreshInfo()
        {
            if (_session == null) return;
            _session.Send(ePacketType.PACKET_GET_HOST_INFO_REQUEST, new RequestGetHostInfo());
        }

    }
}
