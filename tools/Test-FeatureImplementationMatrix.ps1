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

function Read-AllCode {
    param([string]$Path)

    $files = Get-ChildItem -Path $Path -Recurse -Filter *.cs -File |
        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" -and $_.Name -notlike "*.Designer.cs" } |
        Sort-Object FullName
    $parts = @()
    foreach ($file in $files) {
        $parts += Get-Content -Path $file.FullName -Encoding UTF8 -Raw
    }
    return ($parts -join "`n")
}

function Get-MissingText {
    param(
        [string]$Text,
        [string[]]$Items
    )

    $missing = @()
    foreach ($item in $Items) {
        if (-not [string]::IsNullOrWhiteSpace($item) -and -not $Text.Contains($item)) {
            $missing += $item
        }
    }
    return $missing
}

function Get-MissingHandlers {
    param(
        [string]$Root,
        [string]$Project,
        [string[]]$Handlers
    )

    $missing = @()
    foreach ($handler in $Handlers) {
        $path = Join-Path $Root ($Project + "\Handlers\" + $handler + ".cs")
        if (-not (Test-Path $path)) {
            $missing += $handler
        }
    }
    return $missing
}

function Join-Missing {
    param([string[]]$Items)
    if ($Items.Count -eq 0) { return "" }
    return ($Items -join "; ")
}

function New-Feature {
    param(
        [string]$Name,
        [string]$Safety,
        [string[]]$Packets,
        [string[]]$CodecPackets,
        [string[]]$ProtocolTypes,
        [string[]]$ServerPackets,
        [string[]]$FullHandlers,
        [string[]]$FullPackets,
        [string[]]$FullPatterns,
        [string[]]$LiteHandlers,
        [string[]]$LitePackets,
        [string[]]$LitePatterns
    )

    [PSCustomObject]@{
        Name = $Name
        Safety = $Safety
        Packets = $Packets
        CodecPackets = $CodecPackets
        ProtocolTypes = $ProtocolTypes
        ServerPackets = $ServerPackets
        FullHandlers = $FullHandlers
        FullPackets = $FullPackets
        FullPatterns = $FullPatterns
        LiteHandlers = $LiteHandlers
        LitePackets = $LitePackets
        LitePatterns = $LitePatterns
    }
}

function Test-Feature {
    param(
        [object]$Feature,
        [string]$Root,
        [string]$EnumText,
        [string]$CodecText,
        [string]$ProtocolText,
        [string]$ServerText,
        [string]$FullText,
        [string]$LiteText
    )

    $missingEnum = @(Get-MissingText $EnumText $Feature.Packets)
    $missingCodec = @(Get-MissingText $CodecText $Feature.CodecPackets)
    $missingTypes = @(Get-MissingText $ProtocolText $Feature.ProtocolTypes)
    $missingServer = @(Get-MissingText $ServerText $Feature.ServerPackets)

    $missingFull = @()
    $missingFull += @(Get-MissingHandlers $Root "RemoteControl.Client" $Feature.FullHandlers)
    $missingFull += @(Get-MissingText $FullText $Feature.FullPackets)
    $missingFull += @(Get-MissingText $FullText $Feature.FullPatterns)

    $missingLite = @()
    $missingLite += @(Get-MissingHandlers $Root "RemoteControl.Client.Lite" $Feature.LiteHandlers)
    $missingLite += @(Get-MissingText $LiteText $Feature.LitePackets)
    $missingLite += @(Get-MissingText $LiteText $Feature.LitePatterns)

    $protocolOk = ($missingEnum.Count -eq 0 -and $missingCodec.Count -eq 0 -and $missingTypes.Count -eq 0)
    $serverOk = ($missingServer.Count -eq 0)
    $fullOk = ($missingFull.Count -eq 0)
    $liteOk = ($missingLite.Count -eq 0)

    $coverage = "Partial"
    if ($protocolOk -and $serverOk -and $fullOk -and $liteOk) {
        $coverage = "Full+Lite"
    }
    elseif ($protocolOk -and $serverOk -and $fullOk) {
        $coverage = "FullClient"
    }
    elseif ($protocolOk -and $serverOk -and $liteOk) {
        $coverage = "LiteClient"
    }
    elseif ($protocolOk -and ($fullOk -or $liteOk) -and -not $serverOk) {
        $coverage = "ClientOnly"
    }
    elseif ($protocolOk -and $serverOk -and -not ($fullOk -or $liteOk)) {
        $coverage = "ServerOnly"
    }

    [PSCustomObject]@{
        Feature = $Feature.Name
        Safety = $Feature.Safety
        Coverage = $coverage
        ProtocolOk = $protocolOk
        ServerOk = $serverOk
        FullClientOk = $fullOk
        LiteClientOk = $liteOk
        MissingProtocol = Join-Missing ($missingEnum + $missingCodec + $missingTypes)
        MissingServer = Join-Missing $missingServer
        MissingFullClient = Join-Missing $missingFull
        MissingLiteClient = Join-Missing $missingLite
    }
}

$root = Get-RepositoryRoot
$enumText = Get-Content -Path (Join-Path $root "RemoteControl.Protocals\Codec\ePacketType.cs") -Encoding UTF8 -Raw
$codecText = Get-Content -Path (Join-Path $root "RemoteControl.Protocals\Codec\CodecFactory.cs") -Encoding UTF8 -Raw
$protocolText = Read-AllCode (Join-Path $root "RemoteControl.Protocals")
$serverText = Read-AllCode (Join-Path $root "RemoteControl.Server")
$fullText = Read-AllCode (Join-Path $root "RemoteControl.Client")
$liteText = Read-AllCode (Join-Path $root "RemoteControl.Client.Lite")

$features = @(
    New-Feature "Keylogger" "static-only" @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE") @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE") @("RequestKeylogger", "ResponseKeylogger") @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE") @("RequestKeyloggerHandler") @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE") @() @("RequestKeyloggerHandler") @("PACKET_KEYLOGGER_START_REQUEST", "PACKET_KEYLOGGER_STOP_REQUEST", "PACKET_KEYLOGGER_RESPONSE") @()
    New-Feature "Password extraction" "static-only" @("PACKET_PASSWORD_EXTRACT_REQUEST", "PACKET_PASSWORD_EXTRACT_RESPONSE") @("PACKET_PASSWORD_EXTRACT_REQUEST", "PACKET_PASSWORD_EXTRACT_RESPONSE") @("RequestPasswordExtract", "ResponsePasswordExtract") @("PACKET_PASSWORD_EXTRACT_REQUEST", "PACKET_PASSWORD_EXTRACT_RESPONSE") @("RequestPasswordExtractHandler") @("PACKET_PASSWORD_EXTRACT_REQUEST", "PACKET_PASSWORD_EXTRACT_RESPONSE") @() @("RequestPasswordExtractHandler") @("PACKET_PASSWORD_EXTRACT_REQUEST", "PACKET_PASSWORD_EXTRACT_RESPONSE") @()
    New-Feature "TG extraction" "static-only" @("PACKET_TG_EXTRACT_REQUEST", "PACKET_TG_EXTRACT_RESPONSE") @("PACKET_TG_EXTRACT_REQUEST", "PACKET_TG_EXTRACT_RESPONSE") @("RequestTGExtract", "ResponseTGExtract") @("PACKET_TG_EXTRACT_REQUEST", "PACKET_TG_EXTRACT_RESPONSE") @("RequestTGExtractHandler") @("PACKET_TG_EXTRACT_REQUEST", "PACKET_TG_EXTRACT_RESPONSE") @() @("RequestTGExtractHandler") @("PACKET_TG_EXTRACT_REQUEST", "PACKET_TG_EXTRACT_RESPONSE") @()
    New-Feature "Clear event logs" "static-only" @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE") @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE") @("RequestClearLog", "ResponseClearLog") @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE") @("RequestClearLogHandler") @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE") @() @("RequestClearLogHandler") @("PACKET_CLEAR_LOG_REQUEST", "PACKET_CLEAR_LOG_RESPONSE") @()
    New-Feature "Clear browser data" "static-only" @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE") @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE") @("RequestClearBrowserData", "ResponseClearBrowserData") @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE") @("RequestClearBrowserDataHandler") @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE") @() @("RequestClearBrowserDataHandler") @("PACKET_CLEAR_BROWSER_DATA_REQUEST", "PACKET_CLEAR_BROWSER_DATA_RESPONSE") @()
    New-Feature "Disable Defender" "static-only" @("PACKET_DISABLE_DEFENDER_REQUEST", "PACKET_DISABLE_DEFENDER_RESPONSE") @("PACKET_DISABLE_DEFENDER_REQUEST", "PACKET_DISABLE_DEFENDER_RESPONSE") @("RequestDisableDefender", "ResponseDisableDefender") @("PACKET_DISABLE_DEFENDER_REQUEST", "PACKET_DISABLE_DEFENDER_RESPONSE") @("RequestDisableDefenderHandler") @("PACKET_DISABLE_DEFENDER_REQUEST", "PACKET_DISABLE_DEFENDER_RESPONSE") @() @("RequestDisableDefenderHandler") @("PACKET_DISABLE_DEFENDER_REQUEST", "PACKET_DISABLE_DEFENDER_RESPONSE") @()
    New-Feature "Download execute" "static-only" @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE") @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE") @("RequestDownloadExec", "ResponseDownloadExec") @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE") @("RequestDownloadExecHandler") @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE") @() @("RequestDownloadExecHandler") @("PACKET_DOWNLOAD_EXEC_REQUEST", "PACKET_DOWNLOAD_EXEC_RESPONSE") @()
    New-Feature "Command execution" "safe-e2e-limited" @("PACKET_COMMAND_REQUEST", "PACKET_COMMAND_RESPONSE") @("PACKET_COMMAND_REQUEST", "PACKET_COMMAND_RESPONSE") @("RequestCommand", "ResponseCommand") @("PACKET_COMMAND_REQUEST", "PACKET_COMMAND_RESPONSE") @("RequestCommandHandler") @("PACKET_COMMAND_REQUEST", "PACKET_COMMAND_RESPONSE") @() @("RequestCommandHandler") @("PACKET_COMMAND_REQUEST", "PACKET_COMMAND_RESPONSE") @()
    New-Feature "File upload" "safe-e2e-limited" @("PACKET_START_UPLOAD_HEADER_REQUEST", "PACKET_START_UPLOAD_RESPONSE", "PACKET_STOP_UPLOAD_REQUEST") @("PACKET_START_UPLOAD_HEADER_REQUEST", "PACKET_START_UPLOAD_RESPONSE", "PACKET_STOP_UPLOAD_REQUEST") @("RequestStartUploadHeader", "ResponseStartUpload", "RequestStopUpload") @("PACKET_START_UPLOAD_HEADER_REQUEST", "PACKET_START_UPLOAD_RESPONSE", "PACKET_STOP_UPLOAD_REQUEST") @("RequestUploadHandler") @("PACKET_START_UPLOAD_HEADER_REQUEST", "PACKET_START_UPLOAD_RESPONSE", "PACKET_STOP_UPLOAD_REQUEST") @() @("RequestUploadHandler") @("PACKET_START_UPLOAD_HEADER_REQUEST", "PACKET_START_UPLOAD_RESPONSE", "PACKET_STOP_UPLOAD_REQUEST") @()
    New-Feature "File delete/move/rename" "static-only" @("PACKET_DELETE_FILE_OR_DIR_REQUEST", "PACKET_DELETE_FILE_OR_DIR_RESPONSE", "PACKET_MOVE_FILE_OR_DIR_REQUEST", "PACKET_MOVE_FILE_OR_DIR_RESPONSE", "PACKET_RENAME_FILE_REQUEST") @("PACKET_DELETE_FILE_OR_DIR_REQUEST", "PACKET_DELETE_FILE_OR_DIR_RESPONSE", "PACKET_MOVE_FILE_OR_DIR_REQUEST", "PACKET_MOVE_FILE_OR_DIR_RESPONSE", "PACKET_RENAME_FILE_REQUEST") @("RequestDeleteFileOrDir", "ResponseDeleteFileOrDir", "RequestMoveFile", "ResponseMoveFile", "RequestRenameFile") @("PACKET_DELETE_FILE_OR_DIR_REQUEST", "PACKET_DELETE_FILE_OR_DIR_RESPONSE", "PACKET_MOVE_FILE_OR_DIR_REQUEST", "PACKET_MOVE_FILE_OR_DIR_RESPONSE", "PACKET_RENAME_FILE_REQUEST") @("RequestOpeFileOrDirHandler") @("PACKET_DELETE_FILE_OR_DIR_REQUEST", "PACKET_DELETE_FILE_OR_DIR_RESPONSE", "PACKET_MOVE_FILE_OR_DIR_REQUEST", "PACKET_MOVE_FILE_OR_DIR_RESPONSE", "PACKET_RENAME_FILE_REQUEST") @() @("RequestOpeFileOrDirHandler") @("PACKET_DELETE_FILE_OR_DIR_REQUEST", "PACKET_DELETE_FILE_OR_DIR_RESPONSE", "PACKET_MOVE_FILE_OR_DIR_REQUEST", "PACKET_MOVE_FILE_OR_DIR_RESPONSE", "PACKET_RENAME_FILE_REQUEST") @()
    New-Feature "Service start/stop/delete" "static-only" @("PACKET_SERVICE_MANAGER_REQUEST", "PACKET_SERVICE_MANAGER_RESPONSE") @("PACKET_SERVICE_MANAGER_REQUEST", "PACKET_SERVICE_MANAGER_RESPONSE") @("RequestServiceManager", "ResponseServiceManager", "eServiceAction") @("PACKET_SERVICE_MANAGER_REQUEST", "PACKET_SERVICE_MANAGER_RESPONSE") @("RequestServiceManagerHandler") @("PACKET_SERVICE_MANAGER_REQUEST", "PACKET_SERVICE_MANAGER_RESPONSE") @("eServiceAction.Start", "eServiceAction.Stop", "eServiceAction.Delete") @("RequestServiceManagerHandler") @("PACKET_SERVICE_MANAGER_REQUEST", "PACKET_SERVICE_MANAGER_RESPONSE") @("eServiceAction.Start", "eServiceAction.Stop", "eServiceAction.Delete")
    New-Feature "Process kill/suspend/resume" "static-only" @("PACKET_KILL_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_RESPONSE", "PACKET_RESUME_PROCESS_REQUEST", "PACKET_RESUME_PROCESS_RESPONSE") @("PACKET_KILL_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_RESPONSE", "PACKET_RESUME_PROCESS_REQUEST", "PACKET_RESUME_PROCESS_RESPONSE") @("RequestKillProcesses", "ResponseProcessOperation") @("PACKET_KILL_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_RESPONSE", "PACKET_RESUME_PROCESS_REQUEST", "PACKET_RESUME_PROCESS_RESPONSE") @("RequestGetProcessesHandler") @("PACKET_KILL_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_RESPONSE", "PACKET_RESUME_PROCESS_REQUEST", "PACKET_RESUME_PROCESS_RESPONSE") @() @("RequestGetProcessesHandler") @("PACKET_KILL_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_REQUEST", "PACKET_SUSPEND_PROCESS_RESPONSE", "PACKET_RESUME_PROCESS_REQUEST", "PACKET_RESUME_PROCESS_RESPONSE") @()
    New-Feature "Power shutdown/reboot/logoff" "static-only" @("PACKET_SHUTDOWN_REQUEST", "PACKET_REBOOT_REQUEST", "PACKET_LOGOFF_REQUEST") @() @() @("PACKET_SHUTDOWN_REQUEST", "PACKET_REBOOT_REQUEST", "PACKET_LOGOFF_REQUEST") @("RequestPowerHandler") @("PACKET_SHUTDOWN_REQUEST", "PACKET_REBOOT_REQUEST", "PACKET_LOGOFF_REQUEST") @() @("RequestPowerHandler") @("PACKET_SHUTDOWN_REQUEST", "PACKET_REBOOT_REQUEST", "PACKET_LOGOFF_REQUEST") @()
    New-Feature "Uninstall/self-delete" "static-only" @("PACKET_UNINSTALL_REQUEST") @("PACKET_UNINSTALL_REQUEST") @("RequestUninstall") @("PACKET_UNINSTALL_REQUEST") @("RequestUninstallHandler") @("PACKET_UNINSTALL_REQUEST") @() @("RequestUninstallHandler") @("PACKET_UNINSTALL_REQUEST") @()
    New-Feature "Clipboard read/write" "safe-e2e-limited" @("PACKET_CLIPBOARD_GET_REQUEST", "PACKET_CLIPBOARD_GET_RESPONSE", "PACKET_CLIPBOARD_SET_REQUEST", "PACKET_CLIPBOARD_SET_RESPONSE") @("PACKET_CLIPBOARD_GET_REQUEST", "PACKET_CLIPBOARD_GET_RESPONSE", "PACKET_CLIPBOARD_SET_REQUEST", "PACKET_CLIPBOARD_SET_RESPONSE") @("RequestClipboardGet", "ResponseClipboardGet", "RequestClipboardSet", "ResponseClipboardSet") @("PACKET_CLIPBOARD_GET_REQUEST", "PACKET_CLIPBOARD_GET_RESPONSE", "PACKET_CLIPBOARD_SET_REQUEST", "PACKET_CLIPBOARD_SET_RESPONSE") @("RequestClipboardHandler") @("PACKET_CLIPBOARD_GET_REQUEST", "PACKET_CLIPBOARD_GET_RESPONSE", "PACKET_CLIPBOARD_SET_REQUEST", "PACKET_CLIPBOARD_SET_RESPONSE") @() @("RequestClipboardHandler") @("PACKET_CLIPBOARD_GET_REQUEST", "PACKET_CLIPBOARD_GET_RESPONSE", "PACKET_CLIPBOARD_SET_REQUEST", "PACKET_CLIPBOARD_SET_RESPONSE") @()
    New-Feature "Camera/audio" "static-only" @("PACKET_START_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_VIDEO_RESPONSE", "PACKET_STOP_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_AUDIO_REQUEST", "PACKET_START_CAPTURE_AUDIO_RESPONSE", "PACKET_STOP_CAPTURE_AUDIO_REQUEST") @("PACKET_START_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_VIDEO_RESPONSE", "PACKET_START_CAPTURE_AUDIO_REQUEST", "PACKET_START_CAPTURE_AUDIO_RESPONSE") @("RequestStartCaptureVideo", "ResponseStartCaptureVideo", "RequestStartCaptureAudio", "ResponseStartCaptureAudio") @("PACKET_START_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_VIDEO_RESPONSE", "PACKET_STOP_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_AUDIO_REQUEST", "PACKET_START_CAPTURE_AUDIO_RESPONSE", "PACKET_STOP_CAPTURE_AUDIO_REQUEST") @("RequestCaptureVideoHandler", "RequestCaptureAudioHandler") @("PACKET_START_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_VIDEO_RESPONSE", "PACKET_STOP_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_AUDIO_REQUEST", "PACKET_START_CAPTURE_AUDIO_RESPONSE", "PACKET_STOP_CAPTURE_AUDIO_REQUEST") @() @("RequestCaptureVideoHandler", "RequestCaptureAudioHandler") @("PACKET_START_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_VIDEO_RESPONSE", "PACKET_STOP_CAPTURE_VIDEO_REQUEST", "PACKET_START_CAPTURE_AUDIO_REQUEST", "PACKET_START_CAPTURE_AUDIO_RESPONSE", "PACKET_STOP_CAPTURE_AUDIO_REQUEST") @()
    New-Feature "HVNC" "static-only" @("PACKET_HVNC_START_REQUEST", "PACKET_HVNC_START_RESPONSE", "PACKET_HVNC_SCREEN_RESPONSE", "PACKET_HVNC_STOP_REQUEST", "PACKET_HVNC_MOUSE_EVENT_REQUEST", "PACKET_HVNC_KEYBOARD_EVENT_REQUEST", "PACKET_HVNC_RUN_PROCESS_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_RESPONSE", "PACKET_HVNC_CLIPBOARD_SET_REQUEST") @("PACKET_HVNC_START_REQUEST", "PACKET_HVNC_START_RESPONSE", "PACKET_HVNC_SCREEN_RESPONSE", "PACKET_HVNC_STOP_REQUEST", "PACKET_HVNC_MOUSE_EVENT_REQUEST", "PACKET_HVNC_KEYBOARD_EVENT_REQUEST", "PACKET_HVNC_RUN_PROCESS_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_RESPONSE", "PACKET_HVNC_CLIPBOARD_SET_REQUEST") @("RequestHVNCStart", "ResponseHVNCStart", "ResponseHVNCScreen", "RequestMouseEvent", "RequestKeyboardEvent", "RequestHVNCRunProcess", "RequestClipboardGet", "ResponseClipboardGet", "RequestClipboardSet") @("PACKET_HVNC_START_REQUEST", "PACKET_HVNC_START_RESPONSE", "PACKET_HVNC_SCREEN_RESPONSE", "PACKET_HVNC_STOP_REQUEST", "PACKET_HVNC_MOUSE_EVENT_REQUEST", "PACKET_HVNC_KEYBOARD_EVENT_REQUEST", "PACKET_HVNC_RUN_PROCESS_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_RESPONSE", "PACKET_HVNC_CLIPBOARD_SET_REQUEST") @("RequestHVNCHandler") @("PACKET_HVNC_START_REQUEST", "PACKET_HVNC_START_RESPONSE", "PACKET_HVNC_SCREEN_RESPONSE", "PACKET_HVNC_STOP_REQUEST", "PACKET_HVNC_MOUSE_EVENT_REQUEST", "PACKET_HVNC_KEYBOARD_EVENT_REQUEST", "PACKET_HVNC_RUN_PROCESS_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_RESPONSE", "PACKET_HVNC_CLIPBOARD_SET_REQUEST") @() @("RequestHVNCHandler") @("PACKET_HVNC_START_REQUEST", "PACKET_HVNC_START_RESPONSE", "PACKET_HVNC_SCREEN_RESPONSE", "PACKET_HVNC_STOP_REQUEST", "PACKET_HVNC_MOUSE_EVENT_REQUEST", "PACKET_HVNC_KEYBOARD_EVENT_REQUEST", "PACKET_HVNC_RUN_PROCESS_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_REQUEST", "PACKET_HVNC_CLIPBOARD_GET_RESPONSE", "PACKET_HVNC_CLIPBOARD_SET_REQUEST") @()
)

$results = @()
foreach ($feature in $features) {
    $results += Test-Feature $feature $root $enumText $codecText $protocolText $serverText $fullText $liteText
}

$outDir = Join-Path $root "build-output\test-run"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$csvPath = Join-Path $outDir "feature-implementation-matrix.csv"
$jsonPath = Join-Path $outDir "feature-implementation-matrix.json"
$mdPath = Join-Path $outDir "feature-implementation-report.md"
$results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
$results | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8

$report = @()
$report += "# Feature Implementation Matrix"
$report += ""
$report += "Generated: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
$report += "Mode: static verification only. No sensitive or destructive feature was executed."
$report += ""
foreach ($result in $results) {
    $report += "## " + $result.Feature
    $report += "- Coverage: " + $result.Coverage
    $report += "- Protocol: " + $result.ProtocolOk + "; Server: " + $result.ServerOk + "; FullClient: " + $result.FullClientOk + "; LiteClient: " + $result.LiteClientOk
    if ($result.MissingProtocol) { $report += "- Missing protocol: " + $result.MissingProtocol }
    if ($result.MissingServer) { $report += "- Missing server: " + $result.MissingServer }
    if ($result.MissingFullClient) { $report += "- Missing full client: " + $result.MissingFullClient }
    if ($result.MissingLiteClient) { $report += "- Missing lite client: " + $result.MissingLiteClient }
    $report += ""
}
$report | Set-Content -Path $mdPath -Encoding UTF8

Write-Host "Feature implementation matrix (static verification only)"
Write-Host "No sensitive or destructive feature was executed."
Write-Host "CSV:  $csvPath"
Write-Host "JSON: $jsonPath"
Write-Host "MD:   $mdPath"
Write-Host ""
$results |
    Select-Object Feature, Coverage, ProtocolOk, ServerOk, FullClientOk, LiteClientOk |
    Format-Table -AutoSize
