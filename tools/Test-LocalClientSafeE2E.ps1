param(
    [string]$RelayHost = "",
    [int]$RelayPort = 0,
    [string]$ProtocolAssembly = "RemoteControl.Protocals\bin\x86\Debug\RemoteControl.Protocals.dll",
    [string]$ClientPathMatch = "",
    [string]$ClientId = "",
    [string]$ClientHostName = "",
    [string]$ClientAvatar = "",
    [string]$NetworkFilter = "",
    [string]$ConfigPath = "config.json"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

if ([Environment]::Is64BitProcess) {
    $x86PowerShell = Join-Path $env:WINDIR "SysWOW64\WindowsPowerShell\v1.0\powershell.exe"
    if (Test-Path $x86PowerShell) {
        $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $PSCommandPath)
        foreach ($item in $PSBoundParameters.GetEnumerator()) {
            $args += "-" + $item.Key
            $args += [string]$item.Value
        }
        & $x86PowerShell @args
        exit $LASTEXITCODE
    }
}

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
    param([string]$Root, [string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return Join-Path $Root $Path
}

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Missing config file: $Path"
    }
    return Get-Content -Path $Path -Encoding UTF8 -Raw | ConvertFrom-Json
}

function New-TestResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )

    [PSCustomObject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
    }
}

function Read-Exact {
    param(
        [System.IO.Stream]$Stream,
        [int]$Count
    )

    $buffer = New-Object byte[] $Count
    $offset = 0
    while ($offset -lt $Count) {
        $read = $Stream.Read($buffer, $offset, $Count - $offset)
        if ($read -le 0) {
            throw "Socket closed while reading."
        }
        $offset += $read
    }
    return ,$buffer
}

function Read-Packet {
    param([System.IO.Stream]$Stream)

    $lengthBytes = Read-Exact -Stream $Stream -Count 4
    $length = [BitConverter]::ToInt32($lengthBytes, 0)
    if ($length -lt 5 -or $length -gt 10485760) {
        throw "Invalid packet length: $length"
    }

    $remaining = Read-Exact -Stream $Stream -Count ($length - 4)
    $packet = New-Object byte[] $length
    [Array]::Copy($lengthBytes, 0, $packet, 0, 4)
    [Array]::Copy($remaining, 0, $packet, 4, $remaining.Length)
    return $packet
}

function Decode-Packet {
    param([byte[]]$Packet)

    $packetType = [RemoteControl.Protocals.ePacketType]0
    $body = $null
    [RemoteControl.Protocals.Codec.CodecFactory]::Instance.DecodeObject(
        $Packet,
        [ref]$packetType,
        [ref]$body)

    [PSCustomObject]@{
        Type = $packetType
        Body = $body
    }
}

function Send-Packet {
    param(
        [System.IO.Stream]$Stream,
        [RemoteControl.Protocals.ePacketType]$Type,
        [object]$Body
    )

    $packet = [RemoteControl.Protocals.Codec.CodecFactory]::Instance.EncodeOject($Type, $Body)
    $Stream.Write($packet, 0, $packet.Length)
    $Stream.Flush()
}

function Wait-ForPacket {
    param(
        [System.IO.Stream]$Stream,
        [RemoteControl.Protocals.ePacketType]$ExpectedType,
        [int]$TimeoutMs = 10000
    )

    $deadline = [DateTime]::UtcNow.AddMilliseconds($TimeoutMs)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $packet = Read-Packet -Stream $Stream
            $decoded = Decode-Packet -Packet $packet
            if ($decoded.Type -eq $ExpectedType) {
                return $decoded
            }
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 100
        }
        catch [System.Net.Sockets.SocketException] {
            Start-Sleep -Milliseconds 100
        }
    }

    throw "Timed out waiting for $ExpectedType"
}

function Get-Count {
    param([object]$Value)

    if ($null -eq $Value) {
        return 0
    }
    return $Value.Count
}

$root = Get-RepositoryRoot
$configFullPath = Resolve-RepoPath -Root $root -Path $ConfigPath
$config = Read-JsonFile -Path $configFullPath
if ([string]::IsNullOrWhiteSpace($RelayHost)) {
    $RelayHost = if (-not [string]::IsNullOrWhiteSpace($config.RelayServerIP)) { $config.RelayServerIP } else { $config.ClientPara.ServerIP }
}
if ($RelayPort -eq 0) {
    $RelayPort = if ([int]$config.RelayServerPort -gt 0) { [int]$config.RelayServerPort } else { [int]$config.ClientPara.ServerPort }
}
if ([string]::IsNullOrWhiteSpace($ClientAvatar) -and $config.ClientPara -ne $null) {
    $ClientAvatar = [string]$config.ClientPara.OnlineAvatar
}
if ([string]::IsNullOrWhiteSpace($NetworkFilter)) {
    $address = $null
    if ([System.Net.IPAddress]::TryParse($RelayHost, [ref]$address)) {
        $NetworkFilter = $RelayHost
    }
}

$protocolPath = Resolve-RepoPath -Root $root -Path $ProtocolAssembly
$protocolDir = Split-Path $protocolPath
Set-Location $protocolDir
Add-Type -Path (Join-Path $protocolDir "Newtonsoft.Json.Lite.dll")
Add-Type -Path $protocolPath
Set-Location $root

$results = New-Object System.Collections.Generic.List[object]
$client = New-Object System.Net.Sockets.TcpClient
$client.ReceiveTimeout = 1000
$client.SendTimeout = 5000

try {
    $client.Connect($RelayHost, $RelayPort)
    $stream = $client.GetStream()

    $handshake = New-Object RemoteControl.Protocals.Relay.RelayHandshake
    $handshake.Role = "controller"
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::CYCLER_RELAY_HANDSHAKE) -Body $handshake

    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::CYCLER_RELAY_CLIENT_LIST_REQUEST) -Body $null
    $listPacket = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::CYCLER_RELAY_CLIENT_LIST_RESPONSE) -TimeoutMs 8000
    $clients = @($listPacket.Body.Clients)
    $target = $null
    $selector = "first online client"
    if (-not [string]::IsNullOrWhiteSpace($ClientId)) {
        $selector = "ClientId=$ClientId"
        $target = $clients | Where-Object { $_.ClientId -eq $ClientId } | Select-Object -First 1
    }
    elseif (-not [string]::IsNullOrWhiteSpace($ClientPathMatch)) {
        $selector = "AppPath contains $ClientPathMatch"
        $target = $clients | Where-Object { $_.AppPath -like "*$ClientPathMatch*" } | Select-Object -First 1
    }
    elseif (-not [string]::IsNullOrWhiteSpace($ClientHostName)) {
        $selector = "HostName contains $ClientHostName"
        $target = $clients | Where-Object { $_.HostName -like "*$ClientHostName*" } | Select-Object -First 1
    }
    elseif (-not [string]::IsNullOrWhiteSpace($ClientAvatar)) {
        $selector = "OnlineAvatar=$ClientAvatar"
        $target = $clients | Where-Object { $_.OnlineAvatar -eq $ClientAvatar } | Select-Object -First 1
    }
    else {
        $target = $clients | Select-Object -First 1
    }

    if ($null -eq $target) {
        throw "Target client not found in relay list. selector=$selector; clients seen=$($clients.Count)"
    }

    $results.Add((New-TestResult -Name "Relay online list" -Status "Passed" -Detail "clients=$($clients.Count), selector=$selector, target=$($target.ClientId), host=$($target.HostName), app=$($target.AppPath)"))

    $select = New-Object RemoteControl.Protocals.Relay.RelaySelectClient
    $select.ClientId = $target.ClientId
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::CYCLER_RELAY_SELECT_CLIENT) -Body $select
    $results.Add((New-TestResult -Name "Relay select client" -Status "Passed" -Detail "clientId=$($target.ClientId)"))

    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_DRIVES_REQUEST) -Body $null
    $drives = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_DRIVES_RESPONSE) -TimeoutMs 10000
    $results.Add((New-TestResult -Name "Get drives" -Status ($(if ($drives.Body.Result) { "Passed" } else { "Failed" })) -Detail "count=$(Get-Count $drives.Body.drives); message=$($drives.Body.Message)"))

    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_DRIVES_EX_REQUEST) -Body $null
    $drivesEx = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_DRIVES_EX_RESPONSE) -TimeoutMs 10000
    $results.Add((New-TestResult -Name "Get drives extended" -Status ($(if ($drivesEx.Body.Result) { "Passed" } else { "Failed" })) -Detail "count=$(Get-Count $drivesEx.Body.Drives); message=$($drivesEx.Body.Message)"))

    $dirReq = New-Object RemoteControl.Protocals.RequestGetSubFilesOrDirs
    $dirReq.parentDir = $root
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_SUBFILES_OR_DIRS_REQUEST) -Body $dirReq
    $dirResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_SUBFILES_OR_DIRS_RESPONSE) -TimeoutMs 10000
    $results.Add((New-TestResult -Name "List directory" -Status ($(if ($dirResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "dirs=$(Get-Count $dirResp.Body.dirs), files=$(Get-Count $dirResp.Body.files); path=$root"))

    $procReq = New-Object RemoteControl.Protocals.RequestGetProcesses
    $procReq.IsSimpleMode = $true
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_PROCESSES_REQUEST) -Body $procReq
    $procResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_PROCESSES_RESPONSE) -TimeoutMs 15000
    $results.Add((New-TestResult -Name "Get processes" -Status ($(if ($procResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "count=$(Get-Count $procResp.Body.Processes); message=$($procResp.Body.Message)"))

    try {
        $netReq = New-Object RemoteControl.Protocals.Request.RequestGetNetworkConnections
        $netReq.IncludeUDP = $false
        $netReq.Filter = $NetworkFilter
        Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_NETWORK_CONNECTIONS_REQUEST) -Body $netReq
        $netResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_NETWORK_CONNECTIONS_RESPONSE) -TimeoutMs 45000
        $results.Add((New-TestResult -Name "Get network connections" -Status ($(if ($netResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "tcp=$($netResp.Body.TcpCount), udp=$($netResp.Body.UdpCount); filter=$NetworkFilter"))
    }
    catch {
        $results.Add((New-TestResult -Name "Get network connections" -Status "Failed" -Detail $_.Exception.Message))
    }

    $hostReq = New-Object RemoteControl.Protocals.Request.RequestGetHostInfo
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_HOST_INFO_REQUEST) -Body $hostReq
    $hostResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_HOST_INFO_RESPONSE) -TimeoutMs 30000
    $hostName = if ($hostResp.Body.Basic -ne $null) { $hostResp.Body.Basic.HostName } else { "" }
    $results.Add((New-TestResult -Name "Get host info" -Status ($(if ($hostResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "host=$hostName; disks=$(Get-Count $hostResp.Body.Disks); adapters=$(Get-Count $hostResp.Body.NetworkAdapters)"))

    $windowReq = New-Object RemoteControl.Protocals.Request.RequestGetWindows
    $windowReq.Filter = ""
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_GET_WINDOWS_REQUEST) -Body $windowReq
    $windowResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_GET_WINDOWS_RESPONSE) -TimeoutMs 15000
    $results.Add((New-TestResult -Name "Get windows" -Status ($(if ($windowResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "count=$($windowResp.Body.TotalCount); message=$($windowResp.Body.Message)"))

    $findReq = New-Object RemoteControl.Protocals.Request.RequestFindWindow
    $findReq.Keyword = "RemoteControl"
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_FIND_WINDOW_REQUEST) -Body $findReq
    $findResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_FIND_WINDOW_RESPONSE) -TimeoutMs 15000
    $results.Add((New-TestResult -Name "Find window" -Status ($(if ($findResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "matches=$(Get-Count $findResp.Body.Windows); keyword=RemoteControl"))

    $regReq = New-Object RemoteControl.Protocals.RequestViewRegistryKey
    $regReq.KeyRoot = [RemoteControl.Protocals.eRegistryHive]::CurrentUser
    $regReq.KeyPath = "Software"
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_VIEW_REGISTRY_KEY_REQUEST) -Body $regReq
    $regResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_VIEW_REGISTRY_KEY_RESPONSE) -TimeoutMs 15000
    $results.Add((New-TestResult -Name "Read registry key" -Status ($(if ($regResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "subkeys=$(Get-Count $regResp.Body.KeyNames), values=$(Get-Count $regResp.Body.ValueNames); HKCU\Software"))

    $svcReq = New-Object RemoteControl.Protocals.Request.RequestServiceManager
    $svcReq.Action = [RemoteControl.Protocals.Request.eServiceAction]::List
    Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_SERVICE_MANAGER_REQUEST) -Body $svcReq
    $svcResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_SERVICE_MANAGER_RESPONSE) -TimeoutMs 30000
    $results.Add((New-TestResult -Name "List services" -Status ($(if ($svcResp.Body.Result) { "Passed" } else { "Failed" })) -Detail "count=$(Get-Count $svcResp.Body.Services); message=$($svcResp.Body.Message)"))

    $screenReq = New-Object RemoteControl.Protocals.RequestStartGetScreen
    $screenReq.fps = 1
    try {
        Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_START_CAPTURE_SCREEN_REQUEST) -Body $screenReq
        $screenResp = Wait-ForPacket -Stream $stream -ExpectedType ([RemoteControl.Protocals.ePacketType]::PACKET_START_CAPTURE_SCREEN_RESPONSE) -TimeoutMs 15000
        $imageLen = if ($screenResp.Body.ImageData -ne $null) { $screenResp.Body.ImageData.Length } else { 0 }
        $results.Add((New-TestResult -Name "Capture screen one frame" -Status ($(if ($screenResp.Body.Result -and $imageLen -gt 0) { "Passed" } else { "Failed" })) -Detail "imageBytes=$imageLen; no image written to disk"))
    }
    finally {
        Send-Packet -Stream $stream -Type ([RemoteControl.Protocals.ePacketType]::PACKET_STOP_CAPTURE_SCREEN_REQUEST) -Body $null
    }

    $results.Add((New-TestResult -Name "Skipped destructive/high-risk actions" -Status "Skipped" -Detail "Not executed: keylogger, password/TG extraction, clear logs/browser data, disable defender, download/exec, command exec, upload/write/delete/move/rename, service start/stop/delete, process kill/suspend/resume, power actions, uninstall, clipboard get/set, audio/video camera, HVNC."))
}
catch {
    $results.Add((New-TestResult -Name "Fatal" -Status "Failed" -Detail $_.Exception.Message))
}
finally {
    try { $client.Close() } catch { }
}

Write-Host "Configured relay safe E2E test"
Write-Host "Config: $configFullPath"
Write-Host "Relay: $RelayHost`:$RelayPort"
Write-Host "Target selector: ClientId='$ClientId', ClientPathMatch='$ClientPathMatch', ClientHostName='$ClientHostName', ClientAvatar='$ClientAvatar'"
Write-Host ""
$results | Format-Table -AutoSize

$failed = @($results | Where-Object { $_.Status -eq "Failed" })
if ($failed.Count -gt 0) {
    exit 1
}
