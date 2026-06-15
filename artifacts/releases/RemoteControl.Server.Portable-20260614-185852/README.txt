RemoteControl Server portable package

Usage:
1. Extract this zip to a local folder.
2. Run RemoteControl.Server.exe.
3. If startup fails, check Log\log.txt in this folder.

Requirement:
- .NET Framework 4.x Full must be installed on the server system.
- Windows Server Core is not supported because this is a WinForms desktop UI.

File download:
- Single files are downloaded directly.
- Folders are packaged on the remote side as .zip and then downloaded.

GPU compatibility:
- Generated clients use multi-mode GDI capture fallback for Intel, AMD, NVIDIA, Basic Display, RDP, and virtual display adapters.
- Locked desktops, secure desktops, and protected DRM surfaces cannot be captured by Windows GDI APIs.

Config:
- config.json is included next to the exe and uses relative SkinPath.
- Update RelayServerIP/RelayServerPort if the customer test server changes.
