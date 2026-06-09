# RemoteControl Architecture Notes

本文档记录当前代码结构和主要运行路径，便于后续在不改变功能的前提下拆分旧代码。

## Solution Overview

`RemoteControl.sln` 是 Visual Studio 2010 格式的旧解决方案，主要项目目标为 .NET Framework 4.0。

项目职责：

- `RemoteControl.Server`：控制端 WinForms 应用。
- `RemoteControl.Client`：完整客户端。
- `RemoteControl.Client.Lite`：轻量客户端。
- `RemoteControl.Client.Excutor`：辅助工具进程。
- `RemoteControl.Protocals`：共享协议、模型、工具。
- `RemoteControl.Audio`：音频采集和播放。
- `RemoteControl.Relay`：独立 .NET 6 中转服务。

`RemoteControl.Relay` 当前不在 `RemoteControl.sln` 中，需要单独构建。

## Main Runtime Flow

当前主要流程是：

1. Relay 启动并监听端口。
2. 控制端连接 Relay，发送 `controller` 握手。
3. 客户端连接 Relay，发送 `client` 握手。
4. Relay 维护在线客户端列表。
5. 控制端选择客户端后，Relay 双向绑定控制端和客户端。
6. 控制端发送请求包。
7. Relay 转发原始包。
8. 客户端 Handler 执行请求并发送响应。
9. Relay 转发响应。
10. 控制端 UI 根据响应刷新界面。

## Server Entry Points

服务端入口：

- `RemoteControl.Server/Program.cs`
- `RemoteControl.Server/FrmMain.cs`
- `RemoteControl.Server/RemoteControlServer.cs`

`FrmMain` 在窗体加载时完成：

- 初始化客户端右键菜单。
- 初始化皮肤菜单。
- 初始化图标。
- 初始化 `RemoteControlServer` 事件。
- 尝试自动连接 Relay。
- 初始化文本框快捷键。
- 初始化音频播放设备。

`RemoteControlServer` 负责：

- 连接 Relay。
- 发送控制端握手。
- 请求在线客户端列表。
- 创建虚拟 `SocketSession`。
- 转发 Relay 数据为 UI 事件。

## Client Entry Points

完整客户端入口：

- `RemoteControl.Client/Program.cs`

轻量客户端入口：

- `RemoteControl.Client.Lite/Program.cs`

完整客户端无参数运行时会：

1. 读取客户端参数。
2. 将自身复制到临时目录。
3. 使用配置中的服务名作为文件名。
4. 以 `/r` 参数重新启动。
5. 原进程退出。

`/r` 运行时会：

1. 检查单实例。
2. 注册 Handler。
3. 连接服务器或 Relay。
4. 发送握手和主机信息。
5. 启动接收线程。
6. 启动心跳线程。
7. 进入保活循环。

## Protocol Frame

协议帧格式：

```text
4 bytes packet length
1 byte packet type
N bytes UTF-8 JSON body
```

编码和解码位置：

- `RemoteControl.Protocals/Codec/CodecFactoryBase.cs`
- `RemoteControl.Protocals/Codec/CodecFactory.cs`
- `RemoteControl.Protocals/Codec/ePacketType.cs`

`CodecFactory` 维护包类型和请求/响应类的映射。

## Relay Protocol

Relay 使用同一帧格式，保留 `200` 之后的包类型：

- `CYCLER_RELAY_HANDSHAKE`
- `CYCLER_RELAY_CLIENT_LIST_REQUEST`
- `CYCLER_RELAY_CLIENT_LIST_RESPONSE`
- `CYCLER_RELAY_SELECT_CLIENT`
- `CYCLER_RELAY_CLIENT_ONLINE`
- `CYCLER_RELAY_CLIENT_OFFLINE`

Relay 项目里有独立的 `PacketCodec`，用于不依赖 .NET Framework 共享库也能解析关键握手和列表包。

## Configuration

服务端配置：

- 文件：`config.json`
- 读取：`RemoteControl.Server/Settings.cs`
- 写入：`Settings.SaveSettings()`

客户端生成参数：

- 模型：`ClientParameters`
- 读写：`ClientParametersManager`
- 存储方式：追加到 exe 尾部。

服务端生成客户端时读取：

- `RemoteControl.Client.dat`

该文件通常由客户端工程通过 ILMerge 合并后生成。

## UI Responsibilities

`FrmMain.cs` 当前聚合了太多职责：

- 客户端列表。
- 右键菜单。
- 文件管理。
- 屏幕监控。
- 视频查看。
- 音频监听。
- 远程终端。
- 注册表。
- 进程管理。
- 服务管理。
- Relay 连接。
- 皮肤切换。
- 响应分发。

建议后续用 partial class 拆分，不改变控件名，不改变事件绑定。

## Client Handler Responsibilities

客户端 Handler 由 `Program.InitHandlers()` 注册。

典型结构：

- 接收包类型。
- 转换请求对象。
- 执行本机操作。
- 构造响应对象。
- 通过 `SocketSession.Send` 返回。

后续可以把 Handler 注册表移到独立类，但不得改变包类型和 Handler 对应关系。

## Lite Client

Lite 客户端特点：

- 输出程序集名仍为 `RemoteControl.Client`。
- 依赖 `Newtonsoft.Json.Lite` 和 `RemoteControl.Protocals` 的压缩嵌入资源。
- 运行时通过 `AssemblyLoader` 解析依赖。
- 功能面比完整客户端更窄。

## Excutor Helper

`RemoteControl.Client.Excutor` 用于辅助功能：

- 消息框。
- 下载器。
- 播放器。
- 黑屏窗口。
- 摄像头采集。

完整客户端通过资源释放或启动辅助程序来完成部分能力。

## Build Notes

主解决方案：

```powershell
msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86
```

Relay：

```powershell
dotnet build RemoteControl.Relay/RemoteControl.Relay.csproj
```

当前 CI 若使用 Ubuntu 直接 `dotnet build` 主仓库，不适合 .NET Framework 4.0 WinForms 主方案。

