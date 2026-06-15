using System;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        /// <summary>
        /// 锁定计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLockComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要锁定计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_LOCK_REQUEST, null);
        }

        /// <summary>
        /// 重启计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRebootComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要重启计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_REBOOT_REQUEST, null);
        }

        /// <summary>
        /// 睡眠计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSleepComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要睡眠计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_SLEEP_REQUEST, null);
        }

        /// <summary>
        /// 关闭计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonShutdownComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要关闭计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_SHUTDOWN_REQUEST, null);
        }

        /// <summary>
        /// 休眠计算机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonHibernateComputer_Click(object sender, EventArgs e)
        {
            if (!IsCurrentSessionValid())
                return;

            if (MsgBox.Question("确定要休眠计算机:" + this.currentSession.SocketId, MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                return;

            PostRequstWithCurrentSession(ePacketType.PACKET_HIBERNATE_REQUEST, null);
        }
    }
}
