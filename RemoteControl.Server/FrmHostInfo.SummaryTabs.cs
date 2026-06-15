using System.Windows.Forms;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Server
{
    public partial class FrmHostInfo
    {
        private void AddBasicTab(ResponseGetHostInfo resp)
        {
            var tabBasic = new TabPage("基本");
            var basicPanel = CreateInfoPanel();
            if (resp.Basic != null)
            {
                AddInfoRow(basicPanel, "主机名", resp.Basic.HostName);
                AddInfoRow(basicPanel, "登录用户", resp.Basic.UserName);
                AddInfoRow(basicPanel, "域/工作组", resp.Basic.DomainOrWorkgroup);
                AddInfoRow(basicPanel, "时区", resp.Basic.TimeZone);
                AddInfoRow(basicPanel, "区域", resp.Basic.Region);
                AddInfoRow(basicPanel, "开机时长", resp.Basic.UpTime);
            }
            tabBasic.Controls.Add(basicPanel);
            _tabControl.TabPages.Add(tabBasic);
        }

        private void AddSystemTab(ResponseGetHostInfo resp)
        {
            var tabSystem = new TabPage("系统");
            var sysPanel = CreateInfoPanel();
            if (resp.System != null)
            {
                AddInfoRow(sysPanel, "系统", resp.System.OSName);
                AddInfoRow(sysPanel, "构建号", resp.System.BuildNumber);
                AddInfoRow(sysPanel, "架构", resp.System.Architecture);
                AddInfoRow(sysPanel, "分辨率", resp.System.ScreenResolution);
            }
            tabSystem.Controls.Add(sysPanel);
            _tabControl.TabPages.Add(tabSystem);
        }

        private void AddHardwareTab(ResponseGetHostInfo resp)
        {
            var tabHardware = new TabPage("硬件");
            var hwPanel = CreateInfoPanel();
            if (resp.Hardware != null)
            {
                AddInfoRow(hwPanel, "CPU", resp.Hardware.CpuName);
                AddInfoRow(hwPanel, "核心", string.Format("{0} 物理 / {1} 逻辑",
                    resp.Hardware.PhysicalCoreCount, resp.Hardware.LogicalCoreCount));
                AddInfoRow(hwPanel, "内存", string.Format("{0:F1} GB 总 / {1:F1} GB 可用 ({2:F1}%)",
                    resp.Hardware.TotalMemoryMB / 1024.0,
                    resp.Hardware.AvailableMemoryMB / 1024.0,
                    (1 - (double)resp.Hardware.AvailableMemoryMB / resp.Hardware.TotalMemoryMB) * 100));
            }
            tabHardware.Controls.Add(hwPanel);
            _tabControl.TabPages.Add(tabHardware);
        }
    }
}
