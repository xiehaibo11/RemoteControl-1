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

function Read-Text {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Missing file: $Path"
    }
    return Get-Content -Path $Path -Encoding UTF8 -Raw
}

function Test-Contains {
    param(
        [string]$Text,
        [string]$Pattern
    )

    return $Text.Contains($Pattern)
}

function New-FeatureResult {
    param(
        [string]$Feature,
        [string[]]$Packets,
        [string[]]$Handlers,
        [string[]]$Requests,
        [string[]]$Responses,
        [string]$Root
    )

    $enumText = Read-Text (Join-Path $Root "RemoteControl.Protocals\Codec\ePacketType.cs")
    $codecText = Read-Text (Join-Path $Root "RemoteControl.Protocals\Codec\CodecFactory.cs")
    $programText = Read-Text (Join-Path $Root "RemoteControl.Client\Program.cs")

    $missingPackets = @()
    foreach ($packet in $Packets) {
        if (-not (Test-Contains -Text $enumText -Pattern $packet)) {
            $missingPackets += $packet
        }
    }

    $missingMappings = @()
    foreach ($packet in $Packets) {
        if (-not (Test-Contains -Text $codecText -Pattern "ePacketType.$packet")) {
            $missingMappings += $packet
        }
    }

    $missingHandlers = @()
    foreach ($handler in $Handlers) {
        $handlerPath = Join-Path $Root ("RemoteControl.Client\Handlers\" + $handler + ".cs")
        if (-not (Test-Path $handlerPath)) {
            $missingHandlers += $handler
            continue
        }
        if (-not (Test-Contains -Text $programText -Pattern ("new " + $handler + "("))) {
            $missingHandlers += ($handler + " not registered")
        }
    }

    $missingRequests = @()
    foreach ($request in $Requests) {
        if (-not (Test-Path (Join-Path $Root ("RemoteControl.Protocals\Request\" + $request + ".cs")))) {
            $missingRequests += $request
        }
    }

    $missingResponses = @()
    foreach ($response in $Responses) {
        if (-not (Test-Path (Join-Path $Root ("RemoteControl.Protocals\Response\" + $response + ".cs")))) {
            $missingResponses += $response
        }
    }

    $passed = (
        $missingPackets.Count -eq 0 -and
        $missingMappings.Count -eq 0 -and
        $missingHandlers.Count -eq 0 -and
        $missingRequests.Count -eq 0 -and
        $missingResponses.Count -eq 0
    )

    return [PSCustomObject]@{
        Feature = $Feature
        Passed = $passed
        MissingPackets = ($missingPackets -join ", ")
        MissingMappings = ($missingMappings -join ", ")
        MissingHandlers = ($missingHandlers -join ", ")
        MissingRequests = ($missingRequests -join ", ")
        MissingResponses = ($missingResponses -join ", ")
    }
}

$root = Get-RepositoryRoot
$features = @(
    @{
        Feature = "Keylogger"
        Packets = @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE")
        Handlers = @("RequestKeyloggerHandler")
        Requests = @("RequestKeylogger")
        Responses = @("ResponseKeylogger")
    },
    @{
        Feature = "Startup write"
        Packets = @("PACKET_WRITE_STARTUP_REQUEST", "PACKET_WRITE_STARTUP_RESPONSE")
        Handlers = @("RequestWriteStartupHandler")
        Requests = @("RequestWriteStartup")
        Responses = @("ResponseWriteStartup")
    },
    @{
        Feature = "Clear event logs"
        Packets = @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE")
        Handlers = @("RequestClearLogHandler")
        Requests = @("RequestClearLog")
        Responses = @("ResponseClearLog")
    },
    @{
        Feature = "Clear browser data"
        Packets = @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE")
        Handlers = @("RequestClearBrowserDataHandler")
        Requests = @("RequestClearBrowserData")
        Responses = @("ResponseClearBrowserData")
    },
    @{
        Feature = "Download execute"
        Packets = @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE")
        Handlers = @("RequestDownloadExecHandler")
        Requests = @("RequestDownloadExec")
        Responses = @("ResponseDownloadExec")
    },
    @{
        Feature = "Arbitrary code/plugin execution"
        Packets = @("PACKET_TRANSPORT_EXEC_CODE_REQUEST", "PACKET_RUN_EXEC_CODE_REQUEST")
        Handlers = @("RequestExecCodeHandler")
        Requests = @("RequestTransportExecCode", "RequestRunExecCode")
        Responses = @()
    },
    @{
        Feature = "Uninstall/self-delete"
        Packets = @("PACKET_UNINSTALL_REQUEST")
        Handlers = @("RequestUninstallHandler")
        Requests = @("RequestUninstall")
        Responses = @()
    }
)

$results = @()
foreach ($feature in $features) {
    $results += New-FeatureResult `
        -Feature $feature.Feature `
        -Packets $feature.Packets `
        -Handlers $feature.Handlers `
        -Requests $feature.Requests `
        -Responses $feature.Responses `
        -Root $root
}

Write-Host "High-risk feature wiring dry-run"
Write-Host "No client binary was started. No system-changing feature was executed."
Write-Host ""
$results | Format-Table -AutoSize

$failed = @($results | Where-Object { -not $_.Passed })
if ($failed.Count -gt 0) {
    exit 1
}

