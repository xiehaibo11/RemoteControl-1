# Customer Requirements Coverage

This document tracks the features visible in the `客户需求/` screenshots against the current implementation. It is intended for verification and backlog planning only.

Run the read-only coverage check:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Test-CustomerRequirementCoverage.ps1
```

The script does not start a client binary and does not send remote commands.

## Implemented Or Wired

- Host function menu: file manager, screen monitor, HD screen, background screen, HVNC, system manager, video, terminal, audio monitor, keylogger start/stop, service manager, registry.
- File manager: drive listing with type, total size, free space, refresh, upload, download, visible/hidden run, compress, decompress, delete, rename, new folder, property dialog, copy full path, play/stop music, remote web download.
- Session menu: logoff, reboot, shutdown, uninstall request.
- Log clearing menu: all, system, security, application.
- Browser cleanup menu: IE, Chrome, Skype, Firefox, 360, QQ, Sogou.
- Protocol mappings exist for the added packet types in `RemoteControl.Protocals`.
- The full `RemoteControl.Client` registers handlers for the added protocol features.
- `主机分享` is attached to the client right-click menu for copy/export.
- `打开网址` is attached to the client right-click menu through the existing open-url request flow.
- `远程聊天` is attached to the existing remote-chat request/response flow.
- `查找窗口` is attached to the existing window-search request/response flow and prompts for a keyword before sending.
- `筛选主机` is attached as a local host-list filter, and `清除查找` clears that filter.
- `附加功能` now contains a customer coverage report and restricted-feature explanation instead of a blank placeholder.

## Partial Or Needs Product Decision

- `RemoteControl.Client.Lite` only supports a reduced feature set: drives, files, terminal, screen, HVNC, upload, and download. Enhanced, cleanup, session, service, browser, and configuration features are not wired there.
- `服务管理` can request and display the service list. Start, stop, and delete helpers exist in code but are not exposed as a complete service-management UI.
- `更改分组` and `更改备注` are local UI-only changes and are not persisted or sent through Relay.
- `开关代理` currently sends a fixed enable request; `代理映射` uses a fixed `127.0.0.1:1080` target.
- `更改配置` and `分辨修改` protocol/client handlers exist, but no screenshot-style right-click menu entry is currently wired.

## Not Implemented In Current Data Model

- The `BOSS_EX` screenshot shows a tabular host list with region, ISP, antivirus, external IP, online QQ, TG, WX/QQ/WX, user state, and computer name/remark. The current Relay model only carries client id, host name, IP, app path, avatar, and online time.
- File-manager `提权运行` is not present in the server context menu, although the protocol enum contains an elevate run mode.

## Restricted From Execution Expansion

- `下载更新` / `下载执行` differentiation is not expanded because it would strengthen remote download-and-run/update behavior.
- File-manager `提权运行` is not exposed because it would strengthen privilege-elevation behavior.
- `更改配置` and `分辨修改` are not added to the right-click menu because they alter remote client or system state.
- Service start/stop/delete UI is not added because it changes remote service state.
- Proxy toggle/mapping parameter UI is not added because it changes remote network settings.
- BOSS_EX fields such as antivirus, online QQ/TG/WX, and user status are not collected because they require sensitive environment/account data collection.

## Safety Boundary

High-risk actions such as key capture, persistence, log clearing, browser-data cleanup, download execution, privilege elevation, and uninstall/self-delete should not be exercised in automated tests on a real workstation. Verification should remain static, mocked, or performed in an isolated, consented lab environment.

## UI Privacy

- Relay connection status is shown without the Relay address in the server title bar and runtime output.
- Relay configuration fields are masked in the settings window.
- Host-share copy/export output does not include the Relay address.
