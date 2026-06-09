param(
    [string]$RelayHost = "",
    [int]$RelayPort = 0,
    [string]$GeneratedClient = "RemoteControl.Client.Generated.exe",
    [string]$ProtocolAssembly = "RemoteControl.Protocals\bin\x86\Debug\RemoteControl.Protocals.dll",
    [switch]$SkipRemoteRelayProtocol
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-RepositoryRoot {
    $current = Get-Location
    while ($current -ne $null) {
        if (Test-Path (Join-Path $current.Path ".git")) {
            return $current.Path
        }
        $current = $current.Parent
    }
    return (Get-Location).Path
}

function Resolve-RepoPath {
    param(
        [string]$Root,
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return Join-Path $Root $Path
}

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Missing file: $Path"
    }
    return Get-Content -Path $Path -Encoding UTF8 -Raw | ConvertFrom-Json
}

function Test-TcpPort {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutMs = 5000
    )

    $client = New-Object Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne($TimeoutMs, $false)) {
            return $false
        }
        $client.EndConnect($async)
        return $true
    }
    finally {
        $client.Close()
    }
}

function New-RelayPacket {
    param(
        [byte]$Type,
        [object]$Body
    )

    if ($null -eq $Body) {
        $bodyBytes = New-Object byte[] 0
    }
    else {
        $json = $Body | ConvertTo-Json -Compress
        $bodyBytes = [Text.Encoding]::UTF8.GetBytes($json)
    }

    $length = 5 + $bodyBytes.Length
    $packet = New-Object byte[] $length
    [BitConverter]::GetBytes([int]$length).CopyTo($packet, 0)
    $packet[4] = $Type
    if ($bodyBytes.Length -gt 0) {
        $bodyBytes.CopyTo($packet, 5)
    }
    return $packet
}

function Send-RelayPacket {
    param(
        [IO.Stream]$Stream,
        [byte]$Type,
        [object]$Body
    )

    $packet = New-RelayPacket -Type $Type -Body $Body
    $Stream.Write($packet, 0, $packet.Length)
    $Stream.Flush()
}

function Read-Exact {
    param(
        [IO.Stream]$Stream,
        [int]$Count
    )

    $buffer = New-Object byte[] $Count
    $read = 0
    while ($read -lt $Count) {
        $size = $Stream.Read($buffer, $read, $Count - $read)
        if ($size -le 0) {
            throw "Socket closed while reading."
        }
        $read += $size
    }
    return $buffer
}

function Read-RelayPacket {
    param([IO.Stream]$Stream)

    $lengthBytes = Read-Exact -Stream $Stream -Count 4
    $length = [BitConverter]::ToInt32($lengthBytes, 0)
    if ($length -lt 5 -or $length -gt 10485760) {
        throw "Invalid packet length: $length"
    }

    $remaining = Read-Exact -Stream $Stream -Count ($length - 4)
    $type = $remaining[0]
    $bodyLength = $length - 5
    if ($bodyLength -gt 0) {
        $bodyBytes = New-Object byte[] $bodyLength
        [Array]::Copy($remaining, 1, $bodyBytes, 0, $bodyLength)
        $body = [Text.Encoding]::UTF8.GetString($bodyBytes)
    }
    else {
        $body = ""
    }

    return [PSCustomObject]@{
        Type = $type
        Length = $length
        Body = $body
    }
}

function Test-RelayProtocol {
    param(
        [string]$HostName,
        [int]$Port
    )

    $controller = New-Object Net.Sockets.TcpClient
    $client = New-Object Net.Sockets.TcpClient
    try {
        $controller.ReceiveTimeout = 7000
        $client.ReceiveTimeout = 7000
        $controller.Connect($HostName, $Port)
        $controllerStream = $controller.GetStream()
        Send-RelayPacket -Stream $controllerStream -Type 200 -Body @{ Role = "controller" }

        $client.Connect($HostName, $Port)
        $clientStream = $client.GetStream()
        Send-RelayPacket -Stream $clientStream -Type 200 -Body @{
            Role = "client"
            HostName = "synthetic-client-check"
            AppPath = "synthetic"
            OnlineAvatar = "test.png"
        }

        $online = Read-RelayPacket -Stream $controllerStream
        Send-RelayPacket -Stream $controllerStream -Type 201 -Body $null
        $list = Read-RelayPacket -Stream $controllerStream

        return [PSCustomObject]@{
            Success = ($online.Type -eq 204 -and $list.Type -eq 202)
            OnlinePacketType = $online.Type
            ListPacketType = $list.Type
            OnlineBody = $online.Body
            ListBody = $list.Body
        }
    }
    finally {
        $controller.Close()
        $client.Close()
    }
}

function Read-GeneratedClientParameters {
    param(
        [string]$Root,
        [string]$ClientPath,
        [string]$AssemblyPath
    )

    $clientFullPath = Resolve-RepoPath -Root $Root -Path $ClientPath
    $assemblyFullPath = Resolve-RepoPath -Root $Root -Path $AssemblyPath
    if (-not (Test-Path $clientFullPath)) {
        throw "Generated client not found: $clientFullPath"
    }
    if (-not (Test-Path $assemblyFullPath)) {
        throw "Protocol assembly not found: $assemblyFullPath"
    }

    $x86PowerShell = Join-Path $env:WINDIR "SysWOW64\WindowsPowerShell\v1.0\powershell.exe"
    $script = @"
Add-Type -Path '$assemblyFullPath'
`$p = [RemoteControl.Protocals.ClientParametersManager]::ReadParameters('$clientFullPath')
`$result = [PSCustomObject]@{
    File = '$clientFullPath'
    HasHeader = (`$p.Header -ne `$null -and `$p.Header.Length -eq 4)
    IP = `$p.GetServerIP()
    Port = `$p.ServerPort
    ServiceName = `$p.ServiceName
    Avatar = `$p.OnlineAvatar
}
`$result | ConvertTo-Json -Compress
"@

    $json = & $x86PowerShell -NoProfile -ExecutionPolicy Bypass -Command $script
    return $json | ConvertFrom-Json
}

$root = Get-RepositoryRoot
$rootConfigPath = Join-Path $root "config.json"
$config = Read-JsonFile -Path $rootConfigPath

if ([string]::IsNullOrWhiteSpace($RelayHost)) {
    $RelayHost = $config.RelayServerIP
}
if ($RelayPort -eq 0) {
    $RelayPort = [int]$config.RelayServerPort
}

$clientParams = Read-GeneratedClientParameters -Root $root -ClientPath $GeneratedClient -AssemblyPath $ProtocolAssembly
$tcpReachable = Test-TcpPort -HostName $RelayHost -Port $RelayPort

$relayProtocol = $null
if (-not $SkipRemoteRelayProtocol) {
    $relayProtocol = Test-RelayProtocol -HostName $RelayHost -Port $RelayPort
}

[PSCustomObject]@{
    RootConfig = $rootConfigPath
    ConfigClientIP = $config.ClientPara.ServerIP
    ConfigClientPort = [int]$config.ClientPara.ServerPort
    ConfigRelayIP = $config.RelayServerIP
    ConfigRelayPort = [int]$config.RelayServerPort
    GeneratedClient = $clientParams.File
    GeneratedClientHasHeader = [bool]$clientParams.HasHeader
    GeneratedClientIP = $clientParams.IP
    GeneratedClientPort = [int]$clientParams.Port
    GeneratedClientServiceName = $clientParams.ServiceName
    GeneratedClientAvatar = $clientParams.Avatar
    TcpReachable = $tcpReachable
    RelayProtocolSuccess = if ($relayProtocol -eq $null) { $null } else { [bool]$relayProtocol.Success }
    RelayOnlinePacketType = if ($relayProtocol -eq $null) { $null } else { $relayProtocol.OnlinePacketType }
    RelayListPacketType = if ($relayProtocol -eq $null) { $null } else { $relayProtocol.ListPacketType }
    ConfigMatchesGeneratedClient = (
        $config.ClientPara.ServerIP -eq $clientParams.IP -and
        [int]$config.ClientPara.ServerPort -eq [int]$clientParams.Port
    )
} | Format-List

