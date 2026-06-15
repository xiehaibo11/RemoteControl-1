# Code Split Management

本文档用于把拆分状态固定下来，目标是让后续写代码时先找到归属文件，再修改代码，避免 `FrmMain`、`Program`、Handler 和工具类重新膨胀。

## Current Gate

当前仓库手写 `.cs` 文件必须满足：

- `0-300` 行：健康范围，可以正常维护。
- `301-500` 行：必须先评估拆分点，再继续新增功能。
- `500+` 行：阻断，除非是生成文件或有明确记录的紧急窄修。

执行检查：

```powershell
powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings -CheckProjectIncludes -FailOnViolation
```

`tools\CodeHealth.ProjectIncludeIgnore.txt` 只允许记录明确不参与编译的源码。当前 Lite HVNC 隐形桌面相关源码保留在目录中但未纳入 Lite 构建，原因是它属于高风险远控能力，不能在拆分管理过程中被无意启用。

需要把 300 行也作为阻断时执行：

```powershell
powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProjectIncludes -FailOnWarnings
```

## Current Split State

当前代码健康检查结果：没有手写 `.cs` 超过 300 行，也没有 500 行以上文件。后续新增代码不应把任何手写文件推过 300 行。

## Ownership Map

`RemoteControl.Server`：

- `FrmMain.cs`：主窗体壳、全局状态、加载/关闭入口。
- `FrmMain.ClientTree.cs`：客户端树、筛选、上线/下线展示。
- `FrmMain.ClientMenu*.cs`：客户端右键菜单，按安装、会话、服务、维护、管理员动作分组。
- `FrmMain.Dashboard*.cs`：主机列表仪表盘、分组和行渲染。
- `FrmMain.FileManager*.cs`：文件列表、右键菜单、远程文件动作。
- `FrmMain.FileTransfer.cs`：上传、删除、新建、传输进度。
- `FrmMain.Packet*.cs`：服务端响应分发，按文件、屏幕、注册表、工具响应分组。
- `FrmMain.Remote*.cs`：远程命令、远程电源、远程会话窗口动作。
- `FrmMain.Registry.cs`：注册表树和注册表值操作。
- `FrmMain.Skin*.cs`：皮肤菜单和皮肤应用。
- `FrmMain.ToolbarActions.cs`：工具栏入口和小型共享 UI helper。
- `RemoteControlServer*.cs`：Relay 连接和客户端会话管理。

`RemoteControl.Client` 与 `RemoteControl.Client.Lite`：

- `Program.cs`：启动骨架和主循环入口。
- `Program.Connection.cs`：连接、接收包和重连逻辑。
- `Program.HandlerRegistry.cs`：请求处理器注册。
- `Program.HostInfo.cs`：主机信息和 Relay 握手数据。
- `Program.Installation.cs`：安装路径/启动相关兼容逻辑。
- `Program.Logging.cs`：日志和附加包读取。
- `Handlers/*Handler.cs`：一类请求一类 Handler；目录下载 ZIP 内部实现留在 `RequestDownloadHandler.ZipStoreWriter.cs`。

`RemoteControl.Audio`：

- `WaveIn*.cs`：录音设备、缓冲项和属性。
- `WaveOut*.cs`：播放设备、播放项、属性和音量。
- `NativeMethods/*`：WinMM P/Invoke 结构和常量。

`RemoteControl.Protocals`：

- `Codec/*`：包类型、编码器和会话。
- `Request/*`、`Response/*`：纯数据模型，不放执行业务。
- `Generate/*`：客户端参数嵌入和读取。
- `Relay/*`：Relay 握手和控制消息。

`RemoteControl.Relay`：

- `RelayServer.cs`：服务入口和主循环。
- `RelayServer.Clients.cs`：客户端集合和绑定关系。
- `PacketCodec.cs`：Relay 包解析和序列化。

## Workflow

新增或修改代码前：

1. 先用 `rg` 搜索已有方法、事件和包类型，避免 partial 重复定义。
2. 按 Ownership Map 选择归属文件；没有明确归属时新建小文件，而不是把代码塞回大文件。
3. 新建 `.cs` 后必须确认旧式 `.csproj` 有 `<Compile Include=...>`。
4. 单个手写文件接近 250 行时优先新建职责文件；超过 300 行前必须拆。
5. WinForms 业务逻辑只写在手写 `.cs` 或 `Frm*.partial.cs` 中，不写入 `.Designer.cs`。
6. 协议改动必须同时检查 `ePacketType`、`CodecFactory`、客户端 Handler 和服务端响应处理。
7. 高风险能力只能做兼容、日志、限制、认证、审计或移除，不新增隐蔽性、绕过、凭据、键盘采集、下载执行增强。

完成修改后：

```powershell
msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86 /v:minimal
dotnet build RemoteControl.Relay\RemoteControl.Relay.csproj --configuration Debug
powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings -CheckProjectIncludes -FailOnViolation
```

## Split Rule

拆分只移动完整方法、字段或私有 helper，原则上不改行为。拆分后必须重新构建，若编译失败，优先检查：

- 新 partial 文件是否已加入 `.csproj`。
- 是否遗漏 `using`。
- 是否出现同名同参数方法。
- 是否移动了 Designer 绑定依赖的事件方法。
