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
    return Get-Content -Path $Path -Raw
}

function Test-AllPatterns {
    param(
        [string]$Text,
        [string[]]$Patterns
    )

    $missing = @()
    foreach ($pattern in $Patterns) {
        if (-not $Text.Contains($pattern)) {
            $missing += $pattern
        }
    }
    return $missing
}

function New-CoverageResult {
    param(
        [string]$Feature,
        [string]$Status,
        [string]$Evidence,
        [string]$Notes
    )

    return [PSCustomObject]@{
        Feature = $Feature
        Status = $Status
        Evidence = $Evidence
        Notes = $Notes
    }
}

function New-PatternCoverageResult {
    param(
        [string]$Feature,
        [string]$Text,
        [string[]]$Patterns,
        [string]$Evidence,
        [string]$Notes
    )

    $missing = @(Test-AllPatterns -Text $Text -Patterns $Patterns)
    if ($missing.Count -eq 0) {
        return New-CoverageResult -Feature $Feature -Status "Implemented" -Evidence $Evidence -Notes $Notes
    }

    return New-CoverageResult `
        -Feature $Feature `
        -Status "Missing" `
        -Evidence $Evidence `
        -Notes ("Missing patterns: " + ($missing -join ", "))
}

$root = Get-RepositoryRoot
$serverMainPath = Join-Path $root "RemoteControl.Server\FrmMain.cs"
$serverDesignerPath = Join-Path $root "RemoteControl.Server\FrmSettings.Designer.cs"
$clientProgramPath = Join-Path $root "RemoteControl.Client\Program.cs"
$relayMessagesPath = Join-Path $root "RemoteControl.Protocals\Relay\RelayMessages.cs"

$serverMain = Read-Text $serverMainPath
$serverDesigner = Read-Text $serverDesignerPath
$clientProgram = Read-Text $clientProgramPath
$relayMessages = Read-Text $relayMessagesPath

$results = @()

$results += New-PatternCoverageResult `
    -Feature "Host function menu" `
    -Text $serverMain `
    -Patterns @("onMenuFileManager", "onMenuScreenCapture", "onMenuHDScreen", "onMenuBackgroundScreen", "onMenuHVNC", "onMenuServiceManager", "onMenuRegistry") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Host right-click capability menu handlers are present."

$results += New-PatternCoverageResult `
    -Feature "File manager context menu" `
    -Text $serverMain `
    -Patterns @("FileMenuDownload_Click", "FileMenuUpload_Click", "FileMenuRunShow_Click", "FileMenuRunHide_Click", "FileMenuCompress_Click", "FileMenuDecompress_Click", "FileMenuProperty_Click", "RequestDownloadWebFile") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Remote file-manager operation handlers are present."

$results += New-PatternCoverageResult `
    -Feature "Host share" `
    -Text $serverMain `
    -Patterns @("onMenuCopyHostShareInfo", "onMenuExportHostShareInfo", "BuildHostShareInfo", "RelayServer=(hidden)") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Share output is present and Relay address is hidden."

$results += New-PatternCoverageResult `
    -Feature "Open URL" `
    -Text $serverMain `
    -Patterns @("onMenuOpenUrl", "PACKET_OPEN_URL_REQUEST") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Menu routes to the existing open-url request."

$results += New-PatternCoverageResult `
    -Feature "Remote chat" `
    -Text $serverMain `
    -Patterns @("onMenuRemoteChat", "PACKET_REMOTE_CHAT_REQUEST", "PACKET_REMOTE_CHAT_RESPONSE") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Menu routes to remote-chat request and response output."

$results += New-PatternCoverageResult `
    -Feature "Find window" `
    -Text $serverMain `
    -Patterns @("onMenuFindWindow", "PACKET_FIND_WINDOW_REQUEST", "PACKET_FIND_WINDOW_RESPONSE") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Menu prompts for a keyword and sends a window-search request."

$results += New-PatternCoverageResult `
    -Feature "Host filter" `
    -Text $serverMain `
    -Patterns @("onMenuFilterHosts", "hostFilterKeyword", "RenderClientTree") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Local filter only; no remote command is sent."

$results += New-PatternCoverageResult `
    -Feature "Session menu" `
    -Text $serverMain `
    -Patterns @("onMenuLogoff", "onMenuReboot", "onMenuShutdown", "onMenuUninstall") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Menu handlers exist. High-risk actions are not exercised by this script."

$results += New-PatternCoverageResult `
    -Feature "Log and browser cleanup menus" `
    -Text $serverMain `
    -Patterns @("onMenuClearAllLogs", "onMenuClearSystemLog", "onMenuClearSecurityLog", "onMenuClearApplicationLog", "onMenuClearChrome", "onMenuClearSogou") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Menu handlers exist. High-risk actions are not exercised by this script."

$results += New-PatternCoverageResult `
    -Feature "Additional feature report" `
    -Text $serverMain `
    -Patterns @("onMenuShowCustomerCoverage", "onMenuShowRestrictedFeatures") `
    -Evidence "RemoteControl.Server\FrmMain.cs" `
    -Notes "Placeholder was replaced with a coverage report and restriction explanation."

$relayPrivacyImplemented = $serverMain.Contains("RelayServer=(hidden)") -and (-not $serverMain.Contains('APP_TITLE + " [')) -and $serverDesigner.Contains("UseSystemPasswordChar = true")
if ($relayPrivacyImplemented) {
    $results += New-CoverageResult -Feature "Relay address UI privacy" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs; RemoteControl.Server\FrmSettings.Designer.cs" -Notes "Relay status/address is hidden from the title/share output and masked in settings."
} else {
    $results += New-CoverageResult -Feature "Relay address UI privacy" -Status "Missing" -Evidence "RemoteControl.Server\FrmMain.cs; RemoteControl.Server\FrmSettings.Designer.cs" -Notes "Relay address privacy patterns were not all found."
}

if ($serverMain.Contains("new RequestDownloadExec { Url = frm.InputText, ShowWindow = false, IsUpdate = true }")) {
    $results += New-CoverageResult -Feature "Download update flag" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Download update sets IsUpdate."
} else {
    $results += New-CoverageResult -Feature "Download update flag" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Not expanded because it strengthens remote download/update execution behavior."
}

if ($serverMain.Contains("eRunFileMode.Elevate") -and $serverMain.Contains("FileMenuRunElevate")) {
    $results += New-CoverageResult -Feature "File manager elevate run" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Elevate run menu exists."
} else {
    $results += New-CoverageResult -Feature "File manager elevate run" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Not exposed because it strengthens privilege-elevation behavior."
}

if ($serverMain.Contains("PACKET_CHANGE_CONFIG_REQUEST") -and $serverMain.Contains("FrmChangeConfig") -and $serverMain.Contains("onMenuChangeConfig")) {
    $results += New-CoverageResult -Feature "Change remote configuration menu" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Configuration menu is wired."
} else {
    $results += New-CoverageResult -Feature "Change remote configuration menu" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Protocol/handler exists, but right-click execution is not expanded because it changes remote client state."
}

if ($serverMain.Contains("PACKET_CHANGE_RESOLUTION_REQUEST") -and $serverMain.Contains("onMenuChangeResolution")) {
    $results += New-CoverageResult -Feature "Change resolution menu" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Resolution menu is wired."
} else {
    $results += New-CoverageResult -Feature "Change resolution menu" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Handler exists, but menu execution is not expanded because it changes remote system state."
}

if ($serverMain.Contains("SendServiceAction") -and $serverMain.Contains("eServiceAction.Start") -and $serverMain.Contains("eServiceAction.Stop") -and $serverMain.Contains("eServiceAction.Delete") -and $serverMain.Contains("onMenuStartService")) {
    $results += New-CoverageResult -Feature "Service start stop delete UI" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Service operation UI is present."
} else {
    $results += New-CoverageResult -Feature "Service start stop delete UI" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "List is available; start/stop/delete UI is not expanded because it changes remote service state."
}

if ($serverMain.Contains('ProxyAddress = "127.0.0.1"') -or $serverMain.Contains("Enable = true")) {
    $results += New-CoverageResult -Feature "Proxy dynamic settings" -Status "Restricted" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "Existing menu uses fixed values; dynamic proxy-writing UI is not expanded because it changes remote network settings."
} else {
    $results += New-CoverageResult -Feature "Proxy dynamic settings" -Status "Implemented" -Evidence "RemoteControl.Server\FrmMain.cs" -Notes "No fixed proxy values found."
}

$bossExFields = @("Antivirus", "OnlineQQ", "TG", "WX", "UserStatus", "Region", "ISP")
$missingBossFields = @()
foreach ($field in $bossExFields) {
    if (-not $relayMessages.Contains($field)) {
        $missingBossFields += $field
    }
}
if ($missingBossFields.Count -eq 0) {
    $results += New-CoverageResult -Feature "BOSS_EX host table fields" -Status "Implemented" -Evidence "RemoteControl.Protocals\Relay\RelayMessages.cs" -Notes "Relay client info model contains screenshot fields."
} else {
    $results += New-CoverageResult -Feature "BOSS_EX host table fields" -Status "DataModelGap" -Evidence "RemoteControl.Protocals\Relay\RelayMessages.cs" -Notes ("Not collected: " + ($missingBossFields -join ", "))
}

Write-Host "Customer requirement coverage dry-run"
Write-Host "No client binary was started. No remote command was sent."
Write-Host ""
$results | Sort-Object Status, Feature | Format-Table -AutoSize

$summary = $results | Group-Object Status | Sort-Object Name | ForEach-Object {
    [PSCustomObject]@{
        Status = $_.Name
        Count = $_.Count
    }
}
Write-Host ""
Write-Host "Summary"
$summary | Format-Table -AutoSize
