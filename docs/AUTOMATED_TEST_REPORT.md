# Automated Test Report

Date: 2026-06-09

## Scope

本次自动化测试覆盖构建、配置、生成客户端参数和 Relay 协议连通性。

没有执行真实客户端二进制，也没有测试高风险远控功能。

## Fix Applied

根目录 `config.json` 已修复为可生成有效客户端的配置：

- Client server IP: `(hidden)`
- Client server port: `(hidden)`
- Relay IP: `(hidden)`
- Relay port: `(hidden)`
- Service name: `Runtime Broker.exe`
- Avatar: `16238_100.png`

修复原因：

- 根目录配置原先是空 IP 和 `0` 端口。
- 从根目录启动服务端或重新生成客户端时，会生成不可连接的客户端参数。

## Build Verification

主解决方案构建命令：

```powershell
& 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe' RemoteControl.sln /p:Configuration=Debug /p:Platform=x86 /verbosity:minimal
```

结果：

- `RemoteControl.Audio` 成功。
- `RemoteControl.Protocals` 成功。
- `RemoteControl.Server` 成功。
- `RemoteControl.Client` 成功。
- `RemoteControl.Client.Excutor` 成功。
- `RemoteControl.Client.Lite` 成功。

Relay 构建命令：

```powershell
dotnet build RemoteControl.Relay\RemoteControl.Relay.csproj --configuration Debug --nologo
```

结果：

- 构建成功。
- 警告：0。
- 错误：0。

## Controller Connection Verification

控制端已从根目录配置启动并连接到远端 Relay。

进程状态：

- Process: `RemoteControl.Server`
- Window title: `远程控制服务端`
- TCP state: `Established`
- Remote endpoint: `(hidden)`

## Generated Client Parameter Verification

测试脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Test-RelayAndClientConfig.ps1
```

结果：

- Generated client: `C:\RemoteControl-1\RemoteControl.Client.Generated.exe`
- Has header: `True`
- IP: `(hidden)`
- Port: `(hidden)`
- Service name: `Runtime Broker.exe`
- Avatar: `16238_100.png`
- Root config matches generated client: `True`

## Relay Protocol Verification

Relay 测试方式：

- 创建合成 controller TCP 连接。
- 发送 `CYCLER_RELAY_HANDSHAKE`。
- 创建合成 client TCP 连接。
- 发送 `CYCLER_RELAY_HANDSHAKE`。
- 验证 controller 收到 client online 包。
- 请求在线 client 列表。
- 验证 list response 包。

结果：

- TCP reachable: `True`
- Relay protocol success: `True`
- Online packet type: `204`
- Client list packet type: `202`

## Code Health Check

测试脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings
```

结果：

- 超过 500 行的手写文件：2 个。
- `RemoteControl.Server\FrmMain.cs`
- `RemoteControl.Protocals\Request\RequestKeyboardEvent.cs`

说明：

- 未映射的协议包多为无 body 的控制包或历史未用包，需要人工按功能确认。
- 该检查是 review-only，不代表编译失败。

## Not Executed

以下功能没有自动化执行：

- 真实客户端启动。
- 键盘记录。
- 写启动项或持久化。
- 日志清理。
- 浏览器数据清理。
- 下载执行。
- 任意代码或插件执行。
- 卸载/自删除。
- 代理修改。
- 权限提升。

原因：

- 这些功能会修改本机状态、采集输入、删除数据、改变系统配置或执行远程代码。
- 本次只验证连接、配置、构建和协议，不执行破坏性或高风险行为。

## High-Risk Feature Wiring Dry Run

测试脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Test-HighRiskFeatureWiring.ps1
```

该脚本只做接线检查，不启动真实客户端，不执行功能本体。

检查范围：

- 协议枚举是否存在。
- `CodecFactory` 映射是否存在。
- Request 模型文件是否存在。
- Response 模型文件是否存在。
- Client Handler 文件是否存在。
- Client Handler 是否在 `Program.InitHandlers()` 中注册。

结果：

- Keylogger：通过。
- Startup write：通过。
- Clear event logs：通过。
- Clear browser data：通过。
- Download execute：通过。
- Arbitrary code/plugin execution：通过。
- Uninstall/self-delete：通过。

说明：

- 以上结果只表示协议和 Handler 接线完整。
- 以上结果不表示已执行真实系统修改。
- 未执行功能本体是刻意限制，避免采集敏感输入、删除系统数据或修改持久化状态。
