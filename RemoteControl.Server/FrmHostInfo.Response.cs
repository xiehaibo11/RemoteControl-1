using System;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public partial class FrmHostInfo
    {
        public void HandleResponse(ResponseGetHostInfo resp)
        {
            this.BeginInvoke((Action)(() =>
            {
                _tabControl.TabPages.Clear();

                AddBasicTab(resp);
                AddSystemTab(resp);
                AddHardwareTab(resp);
                AddDiskTab(resp);
                AddNetworkTab(resp);
                AddInstalledSoftwareTab(resp);
                AddSecuritySoftwareTab(resp);

                _statusLabel.Text = "更新于 " + (resp.CollectedAt ?? DateTime.Now.ToString("HH:mm:ss"));
            }));
        }

    }
}
