using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmCaptureScreen
    {
        #region 鼠标操作事件
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseDown;
                req.MouseLocation = e.Location;
                this.oSession.Send(ePacketType.PACKET_MOUSE_EVENT_REQUEST, req);
                Console.WriteLine(req);
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
                this.oSession.Send(ePacketType.PACKET_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseMove;
                req.MouseLocation = e.Location;
                this.oSession.Send(ePacketType.PACKET_MOUSE_EVENT_REQUEST, req);
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
                this.oSession.Send(ePacketType.PACKET_MOUSE_EVENT_REQUEST, req);
            }
        }
        #endregion

        #region 键盘操作事件

        private void FrmCaptureScreen_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyDown;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                this.oSession.Send(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, req);
            }
        }

        private void FrmCaptureScreen_KeyUp(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyUp;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                this.oSession.Send(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, req);
            }
        }
        #endregion

        #region 帧率选择
        /// <summary>
        /// 不同的帧率的点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemFPS_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item != null && item.Tag != null)
            {
                var parent = this.toolStripSplitButton2;
                for (int i = 0; i < parent.DropDownItems.Count; i++)
                {
                    var mItem = parent.DropDownItems[i] as ToolStripMenuItem;
                    if (mItem != null)
                    {
                        mItem.Checked = false;
                    }
                }

                int fps = Convert.ToInt32(item.Tag);
                item.Checked = true;
                RequestStartGetScreen req = new RequestStartGetScreen();
                req.fps = fps;
                oSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
            }
        }

        /// <summary>
        /// 帧率选择点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripSplitButton2_ButtonClick(object sender, EventArgs e)
        {
            toolStripSplitButton2.ShowDropDown();
        }
        #endregion

        #region 捕捉操作选择

        /// <summary>
        /// 捕获鼠标操作按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemCaptureMouse_Click(object sender, EventArgs e)
        {
            toolStripMenuItemCaptureMouse.Checked = !toolStripMenuItemCaptureMouse.Checked;
            _isCaptureMouse = toolStripMenuItemCaptureMouse.Checked;
        }

        /// <summary>
        /// 捕获键盘操作按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemCaptureKeyboard_Click(object sender, EventArgs e)
        {
            toolStripMenuItemCaptureKeyboard.Checked = !toolStripMenuItemCaptureKeyboard.Checked;
            _isCaptureKeyboard = toolStripMenuItemCaptureKeyboard.Checked;
        }

        /// <summary>
        /// 捕获操作点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            toolStripSplitButton1.ShowDropDown();
        }
        #endregion

        #region 窗体关闭前事件

        /// <summary>
        /// 窗体关闭前事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmCaptureScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            SendStopCaptureRequest();

            Image oldImage = this.pictureBox1.Image;
            this.pictureBox1.Image = null;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        #endregion

        private void ctrlAltDelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Win32API.keybd_event(0x11, 0, 0, 0);
            Win32API.keybd_event(18, 0, 0, 0);
            Win32API.keybd_event(0x2E, 0, 0, 0);
        }
    }
}
