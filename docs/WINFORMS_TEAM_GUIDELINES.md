# C# WinForms 团队编码与开发规范

本文档适用于 `RemoteControl.Server` 等 C# WinForms 项目。所有窗体、菜单、控件事件和 Designer 相关修改都必须遵守本规范。

## 1. 命名空间与命名规范

### 窗体与控件命名

- 主窗体必须命名为 `FrmMain`。
- 其他窗体类名必须以 `Frm` 开头，例如 `FrmSetting`、`FrmHostInfo`。
- 不要随意重命名已有窗体、控件、资源名或事件名，除非同时更新 `.Designer.cs`、`.resx`、项目文件和所有引用点。

### 事件处理函数命名

- 由 Visual Studio 窗体设计器自动生成的事件处理函数，必须保持默认命名格式。
- 示例：`button1_Click`、`menuItemStartup_Click`、`toolStripButtonSettings_Click`。
- 禁止手动编写与设计器可能冲突的、模糊的业务函数名，例如 `onMenuWriteStartup`。
- 菜单或按钮绑定事件时，优先通过 Visual Studio 设计器双击控件生成事件方法，不要手写事件签名后再手动绑定。

## 2. 窗体设计器安全守则

### 禁止手动修改 `.Designer.cs`

- `FrmMain.Designer.cs` 是 Visual Studio 自动生成的代码。
- 任何手写业务逻辑、自定义方法、自定义变量都绝对不允许写入 `.Designer.cs`。
- 所有自定义方法、字段、业务逻辑必须写在主要的 `FrmMain.cs` 文件中。
- 如果需要拆分大文件，只能拆分手写逻辑到 `FrmMain.*.cs` partial 文件，不能把业务逻辑放进 `.Designer.cs`。

### 避免重复定义，防止 `CS0111`

- 在 `FrmMain.cs` 或任意 `partial class FrmMain` 文件中新增方法前，必须全局搜索该方法名。
- 搜索范围必须包括：
  - `FrmMain.cs`
  - `FrmMain.*.cs`
  - `FrmMain.Designer.cs`
- 确认不存在同名且参数相同的方法后，才允许新增。
- 如果需要复用已有事件逻辑，优先把逻辑合并到已有事件处理函数中，不要再创建一个相同签名的方法。

### 事件绑定规则

- 设计器已有绑定的事件，不要在代码中重复绑定。
- 新菜单或新按钮的事件，优先通过设计器生成。
- 如果确实需要代码动态创建控件，事件处理函数命名必须清晰、唯一，并在提交前全局搜索确认无冲突。

## 3. 常见编译错误排查指南

### `CS0111`

错误形式：

```text
Type '...' already defines a member called '...' with the same parameter types
```

发生原因：

- 同一个类中存在两个方法名和参数完全相同的方法。
- `partial` 类会被编译成同一个类，所以冲突可能分散在多个 `.cs` 文件中。
- 常见触发方式包括误复制代码、手写方法与设计器自动生成方法重名、拆分 `FrmMain.cs` 时重复保留了同一段方法。

修复流程：

1. 在 Visual Studio 中右键点击报错的方法名，选择 “转到定义 (Go To Definition)”。
2. 检查所有冲突位置，注意区分 `.cs` 文件和 `.Designer.cs` 文件。
3. 如果是多余重复代码，直接删除重复方法。
4. 如果两处代码都有用，必须重命名其中一个方法，或将逻辑合并到一个方法中。
5. 修复后重新构建 `RemoteControl.sln`，确认没有新的重复定义错误。

### Designer 相关错误

如果错误来自 `.Designer.cs`：

- 不要直接手改 `.Designer.cs`。
- 先检查是否在手写代码中创建了同名字段、同名控件或同名事件处理函数。
- 如果设计器文件已经被污染，应优先用 Visual Studio 设计器重新生成，或从版本控制中恢复后再重新做业务修改。

## 4. 提交前检查清单

- 是否修改了 `.Designer.cs`？如果是，确认这是设计器生成的变化，不是手写业务逻辑。
- 是否新增了事件处理函数？如果是，确认已全局搜索无同名同参数方法。
- 是否新增或拆分了 `FrmMain.*.cs`？如果是，确认项目文件已包含该文件，且没有重复方法。
- 是否通过 Visual Studio 或 MSBuild 完成一次构建验证？
- 是否把业务逻辑保留在手写 `.cs` 文件中，而不是 `.Designer.cs` 中？

## 5. 推荐目录结构

`FrmMain` 只能作为 UI 入口，不应继续作为所有业务代码的容器。后续重构建议逐步向以下结构收敛：

```text
RemoteControl.Server
|
+-- UI/
|   +-- FrmMain.UI.cs          # 只放 UI 初始化
|   +-- FrmMain.Events.cs      # 只放事件绑定
|
+-- Modules/
|   +-- Menu/
|   |   +-- MenuAdminService.cs
|   |   +-- MenuMaintenanceService.cs
|   |
|   +-- Remote/
|   |   +-- RemoteCommandService.cs
|   |
|   +-- Tree/
|       +-- ClientTreeService.cs
|
+-- Controllers/
|   +-- ClientController.cs
|
+-- Core/
|   +-- Logger.cs
|   +-- Dispatcher.cs
|
+-- FrmMain.cs                 # 只保留壳和初始化
```

推荐职责划分：

- `UI/`：窗体布局、控件初始化、事件绑定，不放业务执行逻辑。
- `Modules/Menu/`：启动、运行、权限、代理、维护类菜单动作。
- `Modules/Remote/`：远程命令、远程控制、消息通信。
- `Modules/Tree/`：客户端列表、UI 树结构刷新、筛选。
- `Controllers/`：协调 UI 与模块服务，不直接操作底层协议细节。
- `Core/`：日志、分发器、跨模块基础能力。

## 6. `partial class` 使用规范

### 禁止行为

- 禁止在多个 `partial` 文件中写同一个方法。
- 禁止通过复制粘贴制造相同签名的方法。
- 禁止多个文件重复定义同一个事件处理函数。
- 禁止把业务逻辑散落到多个 `FrmMain.*.cs` 文件里互相调用。

### 允许范围

`FrmMain partial` 只允许承担以下职责：

| 文件 | 作用 |
| --- | --- |
| `FrmMain.cs` | 主入口和初始化 |
| `FrmMain.UI.cs` | 控件初始化 |
| `FrmMain.Events.cs` | 事件绑定 |
| `FrmMain.State.cs` | 状态变量 |

强制规则：一个方法只能存在于一个文件中。新增方法前必须全局搜索方法名，确认没有同名同参数方法。

## 7. 方法与事件规范

### 方法命名

方法命名必须统一使用 `PascalCase`。

错误示例：

```csharp
onMenuWriteStartup
buttonsendmessage_click
```

正确示例：

```csharp
OnMenuWriteStartup
ButtonSendMessage_Click
RenderClientTree
DoOutput
```

### 事件绑定

禁止行为：

- 多个地方绑定同一个事件。
- 事件逻辑写在多个文件。
- 设计器已绑定事件后，再在手写代码中重复绑定。

推荐方式：

```csharp
// FrmMain.Events.cs
buttonSendMessage.Click += ButtonSendMessage_Click;
```

## 8. 禁止重复定义规则

以下类型的方法只能存在一次：

- `RenderClientTree()`
- `DoOutput()`
- `SendXXX()`
- `ButtonXXX_Click()`
- 任意设计器事件处理函数

建议在项目文件中启用重复定义相关错误的严格检查：

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<WarningsAsErrors>CS0111;CS0121</WarningsAsErrors>
```

说明：`CS0111` 本身是编译错误，必须修复；`CS0121` 通常说明调用目标不明确，也应作为阻断问题处理。

## 9. 业务模块拆分规范

当前最大问题是 `FrmMain` 容易演变成 God Object。后续所有新增功能都应优先进入模块层，而不是继续堆进 `FrmMain`。

建议拆分模块：

- `Menu` 模块：启动、运行、权限、代理。
- `Remote` 模块：执行命令、远程控制、消息通信。
- `Tree` 模块：客户端列表、UI 树结构刷新。
- `Maintenance` 模块：重启、注销、卸载、日志。

`FrmMain` 的职责应限制为：

- 初始化 UI。
- 调用 Controller。
- 接收 Controller 返回的 UI 状态。
- 不直接承载复杂业务规则。

## 10. 重构路线

### Phase 1：止血

- 删除重复方法，先解决 `CS0111`。
- 合并失控的 `partial` 文件。
- 保证工程能稳定编译。

### Phase 2：拆模块

- 拆出 `FrmMain.RemoteCommands`。
- 独立 Tree 逻辑。
- 独立 Menu 逻辑。

### Phase 3：解耦

- 引入 Service 层。
- `FrmMain` 只做 UI 入口。
- 业务逻辑由 Controller 和 Service 承载。

### Phase 4：规范化

- 命名统一为 `PascalCase`。
- 事件集中绑定。
- 禁止跨文件重复逻辑。

## 11. 当前项目最大风险点

如果继续把 `FrmMain` 当作代码容器，会出现：

- 每次改 UI 都可能影响业务逻辑。
- 每次编译都可能出现 `CS0111` 或 `CS0121`。
- `partial` 文件越加越乱。
- 最终无法维护和安全重构。

一句话原则：`FrmMain` 是 UI 入口，不是业务代码容器。`partial class` 只能用于清晰拆分职责，不能用于隐藏重复逻辑。
