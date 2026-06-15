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

# Create struct instance
$para = New-Object ClientParameters
$para.InitHeader()
$para.SetServerIP("203.91.76.159")
$para.ServerPort = 10010
$para.OnlineAvatar = "16238_100.png"
$para.ServiceName = "RemoteControlClient.exe"

# Serialize to bytes
$ptr = [System.Runtime.InteropServices.Marshal]::AllocHGlobal($size)
[System.Runtime.InteropServices.Marshal]::StructureToPtr($para, $ptr, $true)
$paraBytes = New-Object byte[] $size
[System.Runtime.InteropServices.Marshal]::Copy($ptr, $paraBytes, 0, $size)
[System.Runtime.InteropServices.Marshal]::FreeHGlobal($ptr)

# Read existing exe
$clientExe = "C:\RemoteControl-1\RemoteControl.Client\bin\Debug\RemoteControl.Client.exe"
$exeData = [System.IO.File]::ReadAllBytes($clientExe)

# Check if params already exist at end
$hasParams = $true
$checkOffset = $exeData.Length - $size
for ($i = 0; $i -lt 4; $i++) {
    if ($exeData[$checkOffset + $i] -ne 0xff) { $hasParams = $false; break }
}
Write-Host "Existing params found: $hasParams"

# Write: strip old params if present, then append new ones
$fs = [System.IO.File]::Open($clientExe, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
if ($hasParams) {
    $fs.Write($exeData, 0, $exeData.Length - $size)
} else {
    $fs.Write($exeData, 0, $exeData.Length)
}
$fs.Write($paraBytes, 0, $paraBytes.Length)
$fs.Close()

Write-Host "Done. New exe size: $((Get-Item $clientExe).Length) bytes"
Write-Host "Verifying readback..."

# Verify
$newData = [System.IO.File]::ReadAllBytes($clientExe)
$readParaBytes = New-Object byte[] $size
[Array]::Copy($newData, $newData.Length - $size, $readParaBytes, 0, $size)
$ptr2 = [System.Runtime.InteropServices.Marshal]::AllocHGlobal($size)
[System.Runtime.InteropServices.Marshal]::Copy($readParaBytes, 0, $ptr2, $size)
$readPara = [System.Runtime.InteropServices.Marshal]::PtrToStructure($ptr2, [Type][ClientParameters])
[System.Runtime.InteropServices.Marshal]::FreeHGlobal($ptr2)

$ip = (New-Object System.Net.IPAddress([BitConverter]::GetBytes([int]$readPara.ServerIP))).ToString()
Write-Host "Read IP: $ip"
Write-Host "Read Port: $($readPara.ServerPort)"
Write-Host "Read ServiceName: $($readPara.ServiceName)"
Write-Host "Header bytes: $($readPara.Header[0].ToString('X2')) $($readPara.Header[1].ToString('X2')) $($readPara.Header[2].ToString('X2')) $($readPara.Header[3].ToString('X2'))"
