using System;
using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void UpdateProcessListView(ResponseGetProcesses resp)
        {
            if (resp.Result == false)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ResponseGetProcesses>(UpdateProcessListView), resp);
                return;
            }
            this.listView3.Items.Clear();
            for (int i = 0; i < resp.Processes.Count; i++)
            {
                var property = resp.Processes[i];
                ListViewItem item = new ListViewItem(property.ProcessName);
                item.SubItems.Add(property.PID.ToString());
                item.SubItems.Add(property.User);
                item.SubItems.Add(property.CPURate.ToString());
                item.SubItems.Add(GetFileSizeDesc((long)(property.PrivateMemory)));
                item.SubItems.Add(property.ThreadCount.ToString());
                item.SubItems.Add(property.ExecutablePath);
                item.SubItems.Add(property.FileDescription);
                item.SubItems.Add(property.CommandLine);

                this.listView3.Items.Add(item);
            }
        }
    }
}
