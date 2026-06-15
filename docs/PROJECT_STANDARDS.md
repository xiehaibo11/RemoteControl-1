# RemoteControl Project Standards

本文档用于统一这个旧项目后续维护、拆分和文档写法。目标不是改变功能，而是让每次修改都能更容易评审、构建和回退。

## 适用范围

本规范适用于以下项目：

- `RemoteControl.Server`
- `RemoteControl.Client`
- `RemoteControl.Client.Lite`
- `RemoteControl.Client.Excutor`
- `RemoteControl.Protocals`
- `RemoteControl.Audio`
- `RemoteControl.Relay`

第三方库、生成文件、`bin/`、`obj/`、`.Designer.cs`、`.resx`、`Settings.Designer.cs` 不按手写代码标准检查，但不要手动改生成文件，除非没有其他办法。

## 维护原则

1. 功能保持优先：重构不应改变请求包、响应包、UI 菜单、配置格式或生成客户端行为。
2. 小步提交优先：每次只拆一个职责边界，例如菜单、文件管理、协议分发、皮肤、Relay 会话。
3. 可验证优先：改动后至少说明本地构建、手动启动或协议映射检查的结果。
4. 兼容旧工程：不要随意改名 `Protocals`、`Excutor`、程序集名、资源名或 WinForms 控件名。
5. 高风险能力不得扩展：不得新增或增强隐蔽运行、持久化、绕过、凭据处理、日志清理、键盘采集、远程下载执行等能力。

## 文件长度标准

手写代码文件建议控制在 300 到 500 行以内。

- `0-300` 行：健康范围。
- `301-500` 行：可接受范围。
- `501-1000` 行：需要计划拆分。
- `1000+` 行：应作为重构优先级。

例外：

- `.Designer.cs` 通常由 WinForms 生成，不强行拆分。
- 协议枚举和请求/响应模型可以略长，但应尽量按功能组拆分。
- 兼容旧版本导致无法拆分时，应在文档中记录原因。

拆分方式：

- WinForms 主窗体优先使用 `partial class` 按职责拆分。
- 协议模型按 `Request`、`Response`、`Relay`、`RegistryTool` 等目录归档。
- Handler 保持一类一文件，文件名与包类型职责一致。
- 工具类按平台能力分层，例如进程、注册表、路径、资源、Win32 API。

拆分归属、300/500 行门禁和 `.csproj` 纳入检查见 `docs/CODE_SPLIT_MANAGEMENT.md`。提交前至少执行：

```powershell
powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings -CheckProjectIncludes -FailOnViolation
```

## 命名规范

保持旧项目已有命名风格：

- 类型、方法、属性使用 `PascalCase`。
- 局部变量、私有字段使用 `camelCase`。
- 窗体类型保留 `Frm*`。
- 请求模型保留 `Request*`。
- 响应模型保留 `Response*`。
- 处理器保留 `*Handler`。
- 不修正历史拼写 `Protocals`、`Excutor`，除非同步更新解决方案、程序集、资源、脚本和打包链路。

## 目录规范

建议目录职责：

- `RemoteControl.Server/`：WinForms 控制端 UI、会话展示、请求发送和响应展示。
- `RemoteControl.Client/`：完整客户端入口、请求处理器和本机能力实现。
- `RemoteControl.Client.Lite/`：轻量客户端和嵌入式依赖加载。
- `RemoteControl.Client.Excutor/`：辅助 UI 和本地采集工具。
- `RemoteControl.Protocals/`：协议枚举、请求/响应模型、编码器、通用工具。
- `RemoteControl.Audio/`：音频采集、播放和编解码。
- `RemoteControl.Relay/`：中转服务，不纳入旧版 `.sln` 的 .NET Framework 构建。
- `docs/`：项目规范、架构说明、重构计划、验证记录。
- `tools/`：只读检查脚本或构建辅助脚本。

## 文档规范

新增或修改功能时，应同步维护文档。

建议文档类型：

- `docs/ARCHITECTURE.md`：整体架构、模块关系、主流程。
- `docs/REFACTORING_GUIDE.md`：拆分策略和不改功能的重构顺序。
- `docs/PROJECT_STANDARDS.md`：本规范。
- `docs/CHANGELOG.md`：后续可选，用于记录版本级变化。
- `docs/VERIFICATION.md`：后续可选，用于记录手动测试和构建环境。

文档写法：

- 先说明目的，再说明范围。
- 对旧代码保留历史名称，不在文档里创造新概念。
- 使用明确路径和类型名。
- 对配置项写出默认值、来源和影响范围。
- 对手动验证写出具体步骤，不写泛泛的“测试通过”。

## 架构文档要求

架构文档至少覆盖：

- 入口点。
- 进程间或网络通信方式。
- 协议帧格式。
- 请求和响应映射位置。
- 配置读取和写入位置。
- 生成客户端模板来源。
- Relay 是否参与当前运行路径。
- 不同项目之间的引用关系。

## 代码注释规范

只在以下场景添加注释：

- Win32 API、P/Invoke、句柄生命周期。
- 协议包格式、二进制布局、兼容要求。
- 旧工程限制，例如 VS2010、.NET Framework 4.0。
- 非显而易见的线程、锁、资源释放逻辑。

不要添加以下注释：

- “设置变量值”这类重复代码含义的注释。
- 为临时调试输出添加解释。
- 用注释替代清晰命名。

## WinForms 拆分规范

`FrmMain.cs` 是当前最主要的超长文件，应优先按 partial 拆分：

- `FrmMain.ClientMenu.cs`：客户端树右键菜单和对应事件。
- `FrmMain.FileManager.cs`：驱动器、目录、文件菜单和上传下载 UI。
- `FrmMain.PacketHandling.cs`：`PacketReceived` 的响应分派。
- `FrmMain.SessionTree.cs`：在线列表、筛选、TreeView 选择和断开。
- `FrmMain.Skin.cs`：皮肤菜单和皮肤切换。
- `FrmMain.Registry.cs`：注册表浏览和操作。
- `FrmMain.RemoteScreens.cs`：屏幕、视频、音频、HVNC 窗口绑定。

拆分时只移动方法，不改方法体逻辑。先移动私有字段依赖少的区块，再移动复杂区块。

## 协议修改规范

协议层必须保持三处一致：

1. `ePacketType` 中的枚举值。
2. `CodecFactory.GetMappings()` 中的类型映射。
3. 客户端 `InitHandlers()` 或服务端响应处理中的分发逻辑。

新增包类型时：

- 不要插入到历史枚举中间，优先追加到末尾，避免改变旧数值。
- Request 和 Response 类型命名必须和包类型对应。
- 请求类只放数据，不放执行逻辑。
- 响应类应继承或匹配现有 `ResponseBase` 风格。

## Handler 规范

Handler 应保持以下结构：

- 一个 Handler 处理一个职责或一组紧密相关的包。
- `Handle` 中只做分发和参数转换。
- 长耗时逻辑放后台线程，但要可停止、可释放资源。
- 响应发送统一走 `SocketSession.Send`。
- 异常要返回响应或记录到控制台，避免吞掉关键信息。

不要在 Handler 中新增跨功能依赖。需要公共能力时放到 `RemoteControl.Protocals.Utilities` 或当前项目的 `Utils`。

## 配置规范

配置来源：

- 服务端默认配置：根目录或运行目录的 `config.json`。
- 客户端生成配置：追加写入客户端 exe 末尾的 `ClientParameters`。
- 服务端 UI 设置：`FrmSettings`。

配置修改要求：

- 不提交真实公网 IP、机器私有路径或个人图标路径。
- 修改 `ClientParameters` 布局时必须确认 `Marshal.SizeOf` 和读写兼容。
- 修改 `config.json` 时说明对服务端启动和客户端生成的影响。

## 构建规范

主工程目标：

```powershell
msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86
```

清理：

```powershell
msbuild RemoteControl.sln /t:Clean /p:Configuration=Debug /p:Platform=x86
```

Relay 单独构建：

```powershell
dotnet build RemoteControl.Relay/RemoteControl.Relay.csproj
```

注意：

- 主解决方案是 .NET Framework 4.0，不适合用 Linux runner 直接构建。
- Relay 是 .NET 6，不在旧版 Visual Studio 2010 方案内。
- `copy.bat` 包含历史硬编码路径，使用前必须改路径。

## 验证规范

没有测试项目时，至少做以下手动验证：

- 服务端可启动。
- Relay 配置为空时服务端不崩溃。
- Relay 配置正确时可连接中转。
- 客户端生成流程能找到 `RemoteControl.Client.dat`。
- 修改协议时，服务端请求和客户端 Handler 都能匹配。
- 修改 UI 时，目标菜单或窗口能打开。

验证记录应包含：

- 环境。
- 命令。
- 结果。
- 未验证项。
- 已知风险。

## 生成产物规范

不建议提交：

- `bin/`
- `obj/`
- `*.pdb`
- 本地生成的 `*.exe`
- `.userprefs`
- `.csproj.user`
- 临时 diff 文件
- 日志文件

确实需要提交二进制模板时，应说明来源和生成方式，例如 `RemoteControl.Client.dat`。

## 安全维护规范

这个项目包含敏感远程控制能力。后续维护只能做以下类型的工作：

- 文档化。
- 构建修复。
- 兼容性修复。
- 日志和错误处理改进。
- 移除或限制高风险能力。
- 增加授权、认证、加密、审计和显式用户同意。

不做以下类型的工作：

- 增强隐蔽性。
- 增强持久化。
- 增强绕过检测。
- 增强未授权访问。
- 增强凭据、日志、键盘采集能力。
- 增强下载执行或任意代码执行能力。

## 评审清单

提交前检查：

- 是否改变协议包编号。
- 是否改变客户端生成模板。
- 是否改变运行参数。
- 是否修改生成文件。
- 是否新增机器相关路径。
- 是否引入新第三方依赖。
- 是否有构建或手动验证记录。
- 是否违反 300 到 500 行文件目标。
- 新增 `.cs` 是否已纳入对应旧式 `.csproj`。
- 是否扩展了高风险能力。

## 渐进升级路线

建议顺序：

1. 固定构建环境和 CI。
2. 添加文档和代码健康检查。
3. 将 `FrmMain.cs` 拆成 partial 文件。
4. 将协议响应分派改成表驱动，但保持包类型和行为不变。
5. 将客户端 Handler 注册整理到专门的注册类。
6. 将 Relay 纳入独立构建文档。
7. 补最小可执行的手动验证脚本。
8. 对敏感能力增加授权、认证和审计。

当前拆分归属和后续新增代码流程见 `docs/CODE_SPLIT_MANAGEMENT.md`。
