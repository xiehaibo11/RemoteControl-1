using System;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteControl.SubController
{
    internal class FrmSubSettings : Form
    {
        private TextBox txtIP;
        private TextBox txtPort;
        private Button btnOk;
        private Button btnCancel;

        public FrmSubSettings()
        {
            InitUI();
            LoadConfig();
        }

        private void InitUI()
        {
            this.Text = "Relay服务器设置";
            this.Size = new Size(380, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblIP = new Label { Text = "Relay IP:", Location = new Point(20, 25), AutoSize = true };
            txtIP = new TextBox { Location = new Point(100, 22), Width = 230 };

            var lblPort = new Label { Text = "Relay Port:", Location = new Point(20, 60), AutoSize = true };
            txtPort = new TextBox { Location = new Point(100, 57), Width = 100 };

            btnOk = new Button { Text = "确定", Location = new Point(160, 100), Size = new Size(75, 28), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "取消", Location = new Point(250, 100), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };

            btnOk.Click += btnOk_Click;

            this.Controls.AddRange(new Control[] { lblIP, txtIP, lblPort, txtPort, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void LoadConfig()
        {
            var cfg = SubControllerConfig.Current;
            txtIP.Text = cfg.RelayServerIP;
            txtPort.Text = cfg.RelayServerPort.ToString();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string ip = txtIP.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("请输入Relay IP地址！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            int port;
            if (!int.TryParse(txtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("端口号无效（1-65535）！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            var cfg = SubControllerConfig.Current;
            cfg.RelayServerIP = ip;
            cfg.RelayServerPort = port;
            SubControllerConfig.Save();
        }
    }
}
