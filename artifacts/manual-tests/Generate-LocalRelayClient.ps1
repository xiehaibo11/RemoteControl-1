$ErrorActionPreference = 'Stop'
$root = 'C:\RemoteControl-1'
$protocol = Join-Path $root 'RemoteControl.Protocals\bin\x86\Debug\RemoteControl.Protocals.dll'
$template = Join-Path $root 'RemoteControl.Server\bin\Debug\RemoteControl.Client.dat'
$out = Join-Path $root 'artifacts\manual-tests\RemoteControl.Client.LocalRelay.Generated.exe'
Add-Type -Path $protocol
$para = New-Object RemoteControl.Protocals.ClientParameters
$para.SetServerIP('127.0.0.1')
$para.ServerPort = 10010
$para.ServiceName = 'RemoteControlClient.Test.exe'
$para.OnlineAvatar = '16238_100.png'
[byte[]]$bytes = [System.IO.File]::ReadAllBytes($template)
[RemoteControl.Protocals.ClientParametersManager]::WriteClientStyle($bytes, [RemoteControl.Protocals.ClientParametersManager+ClientStyle]::Normal)
[RemoteControl.Protocals.ClientParametersManager]::WriteParameters($bytes, $out, $para)
$readBack = [RemoteControl.Protocals.ClientParametersManager]::ReadParameters($out)
$style = [RemoteControl.Protocals.ClientParametersManager]::ReadClientStyle($out)
[PSCustomObject]@{
    Output = $out
    Length = (Get-Item $out).Length
    ServerIP = $readBack.GetServerIP()
    ServerPort = $readBack.ServerPort
    ServiceName = $readBack.ServiceName
    OnlineAvatar = $readBack.OnlineAvatar
    Style = $style.ToString()
} | Format-List
