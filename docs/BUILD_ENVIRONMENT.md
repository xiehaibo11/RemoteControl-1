# Build Environment

本文档记录当前项目需要的构建工具，以及本机验证过的命令。

## Required Tools

主解决方案需要：

- Windows
- .NET Framework 4.x runtime/build tools
- MSBuild for .NET Framework
- Visual Studio 2010 或兼容的 MSBuild 工具链
- `Libs/` 中的第三方 DLL

Relay 项目需要：

- .NET SDK 6.0
- NuGet 网络访问
- `Newtonsoft.Json` 13.0.3

## Current Verified Tools

本机已验证：

- `.NET SDK 6.0.428`
- `.NET Runtime 6.0.36`
- `Microsoft.AspNetCore.App 6.0.36`
- `Microsoft.WindowsDesktop.App 6.0.36`
- `.NET Framework MSBuild 4.8.9221.0`

`dotnet.exe` 安装位置：

```text
C:\Program Files\dotnet\dotnet.exe
```

## Installation Notes

`winget` 安装命令曾长时间无返回。

`choco install dotnet-6.0-sdk` 曾因 Microsoft 官方安装包下载中断失败，日志错误为 `unexpected EOF`。

最终使用 Windows BITS 从 Microsoft 官方地址下载完整安装包，然后静默安装成功：

```powershell
Start-BitsTransfer -Source <dotnet-sdk-url> -Destination C:\Users\Administrator\Downloads\dotnet-sdk-6.0.428-win-x64.exe
```

安装命令：

```powershell
Start-Process -FilePath C:\Users\Administrator\Downloads\dotnet-sdk-6.0.428-win-x64.exe -ArgumentList @('/install','/quiet','/norestart') -Wait
```

## Build Commands

主解决方案：

```powershell
& 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe' RemoteControl.sln /p:Configuration=Debug /p:Platform=x86 /verbosity:minimal
```

Relay：

```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')
dotnet build RemoteControl.Relay\RemoteControl.Relay.csproj --configuration Debug
```

## Verification Result

主解决方案构建结果：

- `RemoteControl.Audio` 成功。
- `RemoteControl.Protocals` 成功。
- `RemoteControl.Server` 成功。
- `RemoteControl.Client` 成功。
- `RemoteControl.Client.Excutor` 成功。
- `RemoteControl.Client.Lite` 成功。

Relay 构建结果：

- NuGet restore 成功。
- `RemoteControl.Relay` 构建成功。
- 输出路径：`RemoteControl.Relay\bin\Debug\net6.0\RemoteControl.Relay.dll`
- 警告：0。
- 错误：0。

## Notes

`.NET 6` 已停止长期支持，但当前 `RemoteControl.Relay.csproj` 目标框架是 `net6.0`，因此本次按项目现状安装 SDK 6.0。

后续升级 Relay 时，可以考虑迁移到受支持的 LTS 版本，并同步更新部署脚本和 CI。

