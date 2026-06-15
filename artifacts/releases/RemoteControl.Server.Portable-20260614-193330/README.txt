RemoteControl Server portable package

Usage:
1. Extract this zip to a local folder.
2. Run RemoteControl.Server.exe.
3. If startup fails, check Log\log.txt in this folder.

Layout:
- The default page is an online host table.
- Select a host row and use the right-click menu for host operations.
- File manager, registry, terminal, and process manager are opened from the right-click menu.

Requirement:
- .NET Framework 4.x Full must be installed on the server system.
- Windows Server Core is not supported because this is a WinForms desktop UI.

File download:
- Single files are downloaded directly.
- Folders are packaged on the remote side as .zip and then downloaded.

Screen capture:
- Use "抓取屏幕" or "屏幕监控" for remote screen capture.
- Generated clients use multi-mode GDI capture fallback for Intel, AMD, NVIDIA, Basic Display, RDP, and virtual display adapters.
- Locked desktops, secure desktops, and protected DRM surfaces cannot be captured by Windows GDI APIs.

Camera:
- Use "摄像头查看" for laptop/desktop camera viewing.
- If no camera is detected, the driver is missing, camera permission is blocked, or another app occupies the camera, the viewer shows a diagnostic image instead of a blank black window.

Config:
- config.json is included next to the exe and uses relative SkinPath.
- Update RelayServerIP/RelayServerPort if the customer test server changes.
