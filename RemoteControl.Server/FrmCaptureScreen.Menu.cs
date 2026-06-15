using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmCaptureScreen
    {
        private void SetupMenuStrip()
        {
            var menuStrip = new MenuStrip();
            menuStrip.Font = new Font("微软雅黑", 9F);

            // 显示器(M)
            var menuMonitor = new ToolStripMenuItem("显示器(&M)");
            menuMonitor.DropDownItems.Add("全屏显示", null, (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
            });
            menuMonitor.DropDownItems.Add("原始大小", null, (s, e) =>
            {
                this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            });
            menuMonitor.DropDownItems.Add(new ToolStripSeparator());
            var mCaptureMouse = new ToolStripMenuItem("捕获鼠标");
            mCaptureMouse.Checked = _isCaptureMouse;
            mCaptureMouse.Click += (s, e) => { _isCaptureMouse = !_isCaptureMouse; mCaptureMouse.Checked = _isCaptureMouse; };
            menuMonitor.DropDownItems.Add(mCaptureMouse);
            var mCaptureKb = new ToolStripMenuItem("捕获键盘");
            mCaptureKb.Checked = _isCaptureKeyboard;
            mCaptureKb.Click += (s, e) => { _isCaptureKeyboard = !_isCaptureKeyboard; mCaptureKb.Checked = _isCaptureKeyboard; };
            menuMonitor.DropDownItems.Add(mCaptureKb);
            menuStrip.Items.Add(menuMonitor);

            // 画面(I)
            var menuImage = new ToolStripMenuItem("画面(&I)");
            var menuFps = new ToolStripMenuItem("帧速");
            int[] fpsOptions = { 1, 3, 5, 10, 15 };
            foreach (int fps in fpsOptions)
            {
                var fpsItem = new ToolStripMenuItem(fps + " fps");
                fpsItem.Tag = fps;
                if (fps == DefaultCaptureFps) fpsItem.Checked = true;
                fpsItem.Click += (s, ev) =>
                {
                    foreach (ToolStripMenuItem mi in menuFps.DropDownItems) mi.Checked = false;
                    ((ToolStripMenuItem)s).Checked = true;
                    int selectedFps = (int)((ToolStripMenuItem)s).Tag;
                    var req = new RequestStartGetScreen();
                    req.fps = selectedFps;
                    oSession.Send(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, req);
                };
                menuFps.DropDownItems.Add(fpsItem);
            }
            menuImage.DropDownItems.Add(menuFps);
            menuImage.DropDownItems.Add("截图保存(&S)", null, toolStripButtonSave_Click);
            menuStrip.Items.Add(menuImage);

            // 查看(V)
            var menuView = new ToolStripMenuItem("查看(&V)");
            menuView.DropDownItems.Add("开始监控", null, toolStripButton2_Click);
            menuView.DropDownItems.Add("Ctrl+Alt+Del", null, ctrlAltDelToolStripMenuItem_Click);
            menuStrip.Items.Add(menuView);

            // 剪贴板(B)
            var menuClip = new ToolStripMenuItem("剪贴板(&B)");
            menuClip.DropDownItems.Add("获取剪贴板(&G)", null, (s, ev) =>
            {
                if (oSession != null)
                    oSession.Send(ePacketType.PACKET_CLIPBOARD_GET_REQUEST, new RequestClipboardGet());
            });
            menuClip.DropDownItems.Add("设置剪贴板(&S)", null, (s, ev) =>
            {
                using (var frm = new FrmInputUrl())
                {
                    frm.Text = "设置剪贴板内容";
                    if (frm.ShowDialog() == DialogResult.OK && oSession != null)
                    {
                        var req = new RequestClipboardSet();
                        req.Text = frm.InputText;
                        oSession.Send(ePacketType.PACKET_CLIPBOARD_SET_REQUEST, req);
                    }
                }
            });
            menuStrip.Items.Add(menuClip);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            menuStrip.BringToFront();
        }
    }
}
