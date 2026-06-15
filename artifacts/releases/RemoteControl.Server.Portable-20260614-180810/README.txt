RemoteControl Server portable package

Usage:
1. Extract this zip to a local folder.
2. Run RemoteControl.Server.exe.
3. If startup fails, check Log\log.txt in this folder.

Requirement:
- .NET Framework 4.x Full must be installed on the server system.
- Windows Server Core is not supported because this is a WinForms desktop UI.

Config:
- config.json is included next to the exe and uses relative SkinPath.
- Update RelayServerIP/RelayServerPort if the customer test server changes.
