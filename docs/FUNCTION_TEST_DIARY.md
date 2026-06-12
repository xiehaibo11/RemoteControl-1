# 功能测试日记

日期：2026-06-12 12:04:23 +08:00

## 测试边界

本轮只执行已授权本机环境中的可见、可恢复操作。以下功能没有在真实机器上执行，只做静态接线检查或标记为未执行：

- 卸载/自删除
- 清理 Windows 日志
- 清理浏览器账号、密码或历史数据
- 键盘记录
- 下载执行/下载更新
- 提权运行
- 写启动项/自启动
- 任意代码或插件执行
- 关机、重启、注销、休眠、睡眠
- 启停/删除系统服务
- 修改代理、修改远端配置、修改分辨率

原因：这些功能会采集输入、删除数据、修改系统状态、执行远程代码或影响当前工作站，不适合在当前真实电脑上做破坏性测试。

## 环境

| 项目 | 值 |
| --- | --- |
| 控制端进程 | `RemoteControl.Server`, PID `31548` |
| 客户端进程 | `RemoteControl.Client`, PID `33888` |
| 控制端路径 | `C:\RemoteControl-1\RemoteControl.Server\bin\Debug\RemoteControl.Server.exe` |
| 客户端路径 | `C:\Users\Administrator\AppData\Local\RemoteControlClient\RemoteControl.Client.exe` |
| Relay | `203.91.76.159:10010` |
| 在线主机 | `DESKTOP-J0ETJ77` |
| TCP 状态 | 控制端和客户端均为 `Established` |

## 实测结果

| 功能 | 结果 | 证据/说明 |
| --- | --- | --- |
| NSIS 打包 | 通过 | `makensis installer.nsi` 已输出 `RemoteControl.Client.Installer.exe` |
| Defender 扫描 | 通过 | `MpCmdRun.exe -Scan -ScanType 3 -File RemoteControl.Client.Installer.exe` 返回 `found no threats` |
| 安装/卸载烟测 | 通过 | 安装到 `artifacts\InstallSmoke` 后静默卸载，退出码均为 `0` |
| 客户端启动 | 通过 | `client.log` 记录客户端启动、读取 IP/端口并连接成功 |
| 控制端连接 Relay | 通过 | 控制端界面显示 `Relay 已连接`，TCP 到 `203.91.76.159:10010` 为 `Established` |
| 客户端连接 Relay | 通过 | `client.log` 记录 `服务器连接成功！`，TCP 到 `203.91.76.159:10010` 为 `Established` |
| 主机上线/选择 | 通过 | 控制端显示 `自动上线：1台`，当前连接为 `DESKTOP-J0ETJ77` |
| 抓取屏幕 | 通过 | 控制端抓屏窗口可见远端桌面；客户端日志有 `PACKET_START_CAPTURE_SCREEN_REQUEST` |
| 保存截图 | 通过 | 已保存到 `artifacts\manual-tests\screen-save-20260612.jpg`，大小 `529242` 字节，尺寸 `2560x1440` |
| 文件目录刷新 | 通过 | 文件管理器显示 C/D/E/F 盘；进入 `C:\` 后列出 `$Recycle.Bin`、`Program Files`、`Users`、`Windows` 等目录 |
| 文件目录请求日志 | 通过 | 客户端日志有 `PACKET_GET_DRIVES_EX_REQUEST` 和 `PACKET_GET_SUBFILES_OR_DIRS_REQUEST` |
| 进程列表 | 未通过 | 控制端发送刷新后列表为空；客户端日志有 `PACKET_GET_PROCESSES_REQUEST`，但 Lite 客户端没有注册进程列表 handler |
| 注册表根节点 | 部分通过 | 控制端可显示 `HKEY_CLASSES_ROOT`、`HKEY_CURRENT_USER`、`HKEY_LOCAL_MACHINE`、`HKEY_USERS`、`HKEY_CURRENT_CONFIG` |
| 注册表远端只读浏览 | 未通过 | 展开 `HKEY_CURRENT_USER` 后无子项返回；客户端日志有 `PACKET_VIEW_REGISTRY_KEY_REQUEST`，但 Lite 客户端没有注册注册表浏览 handler |
| 远程聊天 | 未通过 | 控制端已发送 `Remote chat test 2026-06-12`；客户端日志有 `PACKET_REMOTE_CHAT_REQUEST`，但 Lite 客户端没有注册远程聊天 handler，无回复窗口/回包 |
| 消息弹窗 | 未通过 | 控制端已发送标题 `RemoteControl test`、内容 `MessageBox test 2026-06-12`；客户端日志有 `PACKET_MESSAGEBOX_REQUEST`，但 Lite 客户端没有注册弹窗 handler，无实际弹窗 |

## Lite 客户端 handler 核对

当前已安装的是客户版 Lite 客户端。`RemoteControl.Client.Lite\Program.cs` 只注册了以下主要 handler：

- 磁盘/目录读取：`PACKET_GET_DRIVES_REQUEST`、`PACKET_GET_DRIVES_EX_REQUEST`、`PACKET_GET_SUBFILES_OR_DIRS_REQUEST`
- 远程命令：`PACKET_COMMAND_REQUEST`
- 抓屏：`PACKET_START_CAPTURE_SCREEN_REQUEST`、`PACKET_STOP_CAPTURE_SCREEN_REQUEST`
- 鼠标/键盘事件：`PACKET_MOUSE_EVENT_REQUEST`、`PACKET_KEYBOARD_EVENT_REQUEST`
- 文件下载/上传基础包：`PACKET_START_DOWNLOAD_REQUEST`、`PACKET_STOP_DOWNLOAD_REQUEST`、`PACKET_START_UPLOAD_HEADER_REQUEST`、`PACKET_START_UPLOAD_RESPONSE`、`PACKET_STOP_UPLOAD_REQUEST`
- 打开文件：`PACKET_OPEN_FILE_REQUEST`

Lite 客户端当前没有注册这些本轮需要的 handler：

- `PACKET_GET_PROCESSES_REQUEST`
- `PACKET_VIEW_REGISTRY_KEY_REQUEST`
- `PACKET_REMOTE_CHAT_REQUEST`
- `PACKET_MESSAGEBOX_REQUEST`

完整客户端 `RemoteControl.Client\Program.cs` 里存在 `RequestMsgBoxHandler` 和 `RequestRemoteChatHandler` 注册，因此“远程聊天/消息弹窗”在完整客户端路径上有实现，但当前 Lite 包不支持。

## 只读/静态检查

| 检查项 | 结果 | 说明 |
| --- | --- | --- |
| 客户需求覆盖脚本 | 通过/部分受限 | `tools\Test-CustomerRequirementCoverage.ps1`：11 项 implemented，6 项 restricted，1 项 data model gap |
| 高风险功能接线脚本 | 接线存在 | `tools\Test-HighRiskFeatureWiring.ps1`：高风险协议/handler 接线存在；没有执行功能本体 |
| Lite 包高风险裁剪 | 已处理 | Lite 构建中已移除 HVNC handler 和 `CreateDesktop` 相关编译项 |

## 未执行的高风险项

| 功能 | 状态 | 原因 |
| --- | --- | --- |
| 卸载 | 未执行 | 会删除当前客户端或触发自删除 |
| 清日志 | 未执行 | 会删除系统审计/事件数据 |
| 清浏览器 | 未执行 | 会删除用户浏览器账号、密码或历史数据 |
| 键盘记录 | 未执行 | 会采集输入内容 |
| 下载执行/下载更新 | 未执行 | 会执行远程下载的文件 |
| 提权 | 未执行 | 会触发权限提升和系统状态改变 |
| 关机/重启/注销/休眠/睡眠 | 未执行 | 会影响当前会话或中断测试 |

## 当前未通过项

| 项目 | 结论 | 建议 |
| --- | --- | --- |
| 进程列表 | Lite 客户端缺 handler | 若客户版需要，补注册只读进程列表 handler，并避免暴露结束进程操作 |
| 注册表只读浏览 | Lite 客户端缺 handler | 若客户版需要，补只读注册表浏览 handler，不加入修改/删除值功能 |
| 远程聊天 | Lite 客户端缺 handler | 可把完整客户端的 `RequestRemoteChatHandler` 迁入 Lite 并注册 |
| 消息弹窗 | Lite 客户端缺 handler | 可把完整客户端的 `RequestMsgBoxHandler` 迁入 Lite 并注册，弹窗内容需明确标注测试/协助用途 |

## 修复记录：控制端抓屏重叠和卡顿

时间：2026-06-12 12:18 +08:00

| 项目 | 结果 | 说明 |
| --- | --- | --- |
| 抓屏窗口递归重叠 | 已修复 | `FrmCaptureScreen` 使用 `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)`，本机连本机时抓屏窗口不再被采集进画面 |
| 控制端主窗口叠进抓屏 | 已修复 | `FrmMain` 同样设置屏幕采集排除，避免主控制窗口出现在本机抓屏画面中 |
| 顶栏抓屏按钮只开窗口不发请求 | 已修复 | `toolStripButton3_Click` 现在会发送 `PACKET_START_CAPTURE_SCREEN_REQUEST`，默认 `fps=5` |
| UI 卡顿/帧积压 | 已优化 | 服务端抓屏窗口改为只渲染最新帧，丢弃积压帧，并释放旧 `Image` |
| 客户端抓屏循环 | 已优化 | Lite/完整客户端都限制 fps 到 `1..10`，停止后可重新启动抓屏线程，并释放每帧截图资源 |
| 图片序列化 | 已优化 | `ResponseBase` 改用 `MemoryStream.ToArray()`，避免发送缓冲区多余字节；解码时克隆 `Bitmap`，避免依赖已释放的流 |
| 验证 | 通过 | 解决方案 `Debug|x86` 构建通过；客户端日志出现新的 `PACKET_START_CAPTURE_SCREEN_REQUEST`；保存验证图 `artifacts\manual-tests\screen-fix-20260612.jpg`，尺寸 `2560x1440` |
| 客户端模板/安装包 | 已更新 | `RemoteControl.Client.dat` 已由新 Lite exe 覆盖，`RemoteControl.Client.Generated.exe` 已重新写入 `203.91.76.159:10010` 参数，NSIS 安装包已重建 |
| Defender 扫描 | 通过 | `RemoteControl.Client.Installer.exe` 扫描结果：`found no threats` |

## 修复记录：显卡优化

时间：2026-06-12 12:38 +08:00

| 项目 | 结果 | 说明 |
| --- | --- | --- |
| 显卡检测 | 已加入 | 客户端通过 `Win32_VideoController` 检测真实硬件显卡；忽略 `Microsoft Basic Display`、RDP、虚拟显示、GameViewer、MuMu、VMware、VirtualBox 等虚拟/软件适配器 |
| 显卡优化抓屏路径 | 已加入 | 检测到真实显卡时使用 native DC + compatible bitmap 的快速 BitBlt 抓屏路径；失败时自动回退原兼容路径 |
| 无显卡/虚拟显卡回退 | 已加入 | 没有真实硬件显卡或 WMI 检测失败时，不报错，不黑屏，继续使用原 `CaptureScreen2()` |
| 客户端调用 | 已更新 | Lite/完整客户端抓屏 handler 改为调用 `ScreenUtil.CaptureScreenOptimized()` |
| 本机显卡 | 检测到 | `NVIDIA GeForce RTX 5060 Ti`；同时检测到 GameViewer/MuMu 虚拟显示并忽略 |
| 验证 | 通过 | 直接调用新 Lite 客户端 `CaptureScreenOptimized()` 保存 `artifacts\manual-tests\gpu-capture-test-20260612.jpg`，尺寸 `2560x1440` |
| 客户端模板/安装包 | 已更新 | GPU 优化后的 Lite exe 已同步到 `RemoteControl.Client.dat` 和 `RemoteControl.Client.Generated.exe`，NSIS 安装包已重建 |
| Defender 扫描 | 通过 | 新 `RemoteControl.Client.Installer.exe` 扫描结果：`found no threats` |
