RemoteControl Server portable package

Usage:
1. Extract this zip to a local folder.
2. Run RemoteControl.Server.exe.
3. If startup fails, check Log\log.txt in this folder.

Main layout:
- The default page is an online host table.
- Right-click the host table for all common operations.
- The Controller menu contains: return to host list, refresh hosts, connect/disconnect Relay, generate client, and settings.
- Select a host row before using host-specific operations.
- File manager, registry, terminal, process manager, screen capture, camera view, and other host actions are opened from the right-click menu.

Client generation:
- Right-click the host table, open Controller, then choose Generate Client.
- The generated client uses RemoteControl.Client.dat from this folder.
- Update config.json or Settings before generating if Relay/server address changes.

Requirement:
- .NET Framework 4.x Full must be installed on the server system.
- Windows Server Core is not supported because this is a WinForms desktop UI.

File download:
- Single files are downloaded directly.
- Folders are packaged on the remote side as .zip and then downloaded.

Screen capture:
- Use screen capture actions for remote desktop capture.
- Generated clients use multi-mode GDI capture fallback for Intel, AMD, NVIDIA, Basic Display, RDP, and virtual display adapters.
- Locked desktops, secure desktops, and protected DRM surfaces cannot be captured by Windows GDI APIs.

Camera:
- Use camera view actions for laptop/desktop camera viewing.
- If no camera is detected, the driver is missing, camera permission is blocked, or another app occupies the camera, the viewer shows a diagnostic image instead of a blank black window.

Config:
- config.json is included next to the exe and uses relative SkinPath.
- Update RelayServerIP/RelayServerPort if the customer test server changes.
