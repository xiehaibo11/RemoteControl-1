# No-Behavior-Change Refactoring Guide

本文档给出旧代码升级顺序。核心要求：只改变结构，不改变功能。

## Current Hotspots

当前超长文件主要是：

- `RemoteControl.Server/FrmMain.cs`
- `RemoteControl.Server/FrmMain.Designer.cs`
- `RemoteControl.Protocals/Request/RequestKeyboardEvent.cs`

其中 `.Designer.cs` 是生成文件，不作为第一优先级。

## Refactoring Rules

1. 先新增文档和检查工具。
2. 再拆 partial class。
3. 再提取纯分发逻辑。
4. 最后考虑服务类抽取。

每一步都应能单独构建。

## Step 1: Split FrmMain With Partial Files

建议新增以下文件：

- `FrmMain.ClientMenu.cs`
- `FrmMain.FileManager.cs`
- `FrmMain.PacketHandling.cs`
- `FrmMain.SessionTree.cs`
- `FrmMain.Skin.cs`
- `FrmMain.Registry.cs`
- `FrmMain.RemoteScreens.cs`

移动规则：

- 只剪切粘贴完整方法。
- 不改方法名。
- 不改字段名。
- 不改事件绑定。
- 不改控件访问。
- 每移动一个文件就编译一次。

## Step 2: Stabilize Packet Handling

`oRemoteControlServer_PacketReceived` 过长时，先拆为私有方法：

- `HandleSessionLifecyclePacket`
- `HandleFileManagerPacket`
- `HandleScreenPacket`
- `HandleVideoPacket`
- `HandleAudioPacket`
- `HandleRegistryPacket`
- `HandleServicePacket`
- `HandleOperationResultPacket`

先保持 `if/else` 顺序，后续再考虑字典分发表。

## Step 3: Handler Registration

完整客户端 `Program.InitHandlers()` 可以抽到：

- `Handlers/RequestHandlerRegistry.cs`

轻量客户端也可抽到：

- `Handlers/LiteRequestHandlerRegistry.cs`

注意：

- 不改变 `Dictionary<ePacketType, IRequestHandler>`。
- 不改变多包复用同一 Handler 的关系。
- 不改变 `OnFireQuit` 注入。

## Step 4: Protocol Mapping Audit

协议变更前后都要检查：

- 枚举是否追加而不是插入。
- `CodecFactory` 是否有映射。
- 客户端是否注册 Handler。
- 服务端是否处理响应。

可以使用 `tools/Measure-CodeHealth.ps1` 辅助检查文件长度，协议一致性仍需人工确认。拆分归属、300/500 行门禁和 `.csproj` 纳入检查见 `docs/CODE_SPLIT_MANAGEMENT.md`。

## Step 5: Build and Manual Verification

每次结构性拆分后验证：

```powershell
msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86
```

如果本机没有 `msbuild`，记录未验证原因。

手动验证建议：

- 启动服务端。
- 打开设置窗口。
- 配置 Relay。
- 刷新在线列表。
- 选择一个测试客户端。
- 打开文件管理。
- 打开远程终端。
- 关闭窗口。

不要在非授权环境运行客户端。

## Rollback Strategy

如果拆分后出现问题：

- 先看是否遗漏 `partial` 文件纳入 `.csproj`。
- 再看是否移动了字段初始化顺序。
- 再看事件绑定是否仍指向原方法。
- 最后回退本次移动，不回退其他用户改动。
