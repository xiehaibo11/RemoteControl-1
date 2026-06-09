param(
    [int]$TargetMaxLines = 500,
    [int]$WarningLines = 300,
    [switch]$IncludeGenerated,
    [switch]$CheckProtocolMappings
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-RepositoryRoot {
    $current = Get-Location
    while ($current -ne $null) {
        $gitPath = Join-Path $current.Path ".git"
        if (Test-Path $gitPath) {
            return $current.Path
        }
        $current = $current.Parent
    }
    return (Get-Location).Path
}

function Test-IsGeneratedFile {
    param([string]$Path)

    if ($IncludeGenerated) {
        return $false
    }

    $fileName = [System.IO.Path]::GetFileName($Path)
    if ($fileName.EndsWith(".Designer.cs", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    if ($fileName.Equals("Settings.Designer.cs", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    if ($Path -match "\\(bin|obj)\\") {
        return $true
    }
    return $false
}

function Get-CodeFiles {
    param([string]$Root)

    Get-ChildItem -Path $Root -Recurse -Filter "*.cs" |
        Where-Object { -not (Test-IsGeneratedFile -Path $_.FullName) } |
        Sort-Object FullName
}

function Get-LineCount {
    param([string]$Path)

    $content = Get-Content -Path $Path -Encoding UTF8 -ErrorAction SilentlyContinue
    if ($null -eq $content) {
        return 0
    }
    return ($content | Measure-Object -Line).Lines
}

function Get-RelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    if ($Path.StartsWith($Root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($Root.Length).TrimStart("\")
    }
    return $Path
}

function Get-Status {
    param([int]$Lines)

    if ($Lines -gt $TargetMaxLines) {
        return "SplitRequired"
    }
    if ($Lines -gt $WarningLines) {
        return "Watch"
    }
    return "OK"
}

function Write-CodeLengthReport {
    param([string]$Root)

    $rows = @()
    foreach ($file in Get-CodeFiles -Root $Root) {
        $lineCount = Get-LineCount -Path $file.FullName
        $rows += [PSCustomObject]@{
            Lines = $lineCount
            Status = Get-Status -Lines $lineCount
            Path = Get-RelativePath -Root $Root -Path $file.FullName
        }
    }

    Write-Host ""
    Write-Host "Code length report"
    Write-Host "Target max lines: $TargetMaxLines"
    Write-Host "Warning lines: $WarningLines"
    Write-Host ""

    $rows |
        Sort-Object Lines -Descending |
        Format-Table Lines, Status, Path -AutoSize

    $tooLong = @($rows | Where-Object { $_.Lines -gt $TargetMaxLines })
    Write-Host ""
    Write-Host ("Files over target: {0}" -f $tooLong.Count)
}

function Get-EnumPacketTypes {
    param([string]$EnumFile)

    $result = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path $EnumFile)) {
        return $result
    }

    $inEnum = $false
    foreach ($line in Get-Content -Path $EnumFile -Encoding UTF8) {
        if ($line -match "enum\s+ePacketType") {
            $inEnum = $true
            continue
        }
        if ($inEnum -and $line -match "^\s*}\s*$") {
            break
        }
        if ($inEnum -and $line -match "^\s*([A-Z][A-Z0-9_]+)\s*(=\s*\d+)?\s*,?\s*(//.*)?$") {
            $result.Add($matches[1])
        }
    }

    return $result
}

function Get-MappedPacketTypes {
    param([string]$CodecFile)

    $result = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path $CodecFile)) {
        return $result
    }

    foreach ($line in Get-Content -Path $CodecFile -Encoding UTF8) {
        if ($line -match "mappings\.Add\(ePacketType\.([A-Z][A-Z0-9_]+)") {
            $result.Add($matches[1])
        }
    }

    return $result
}

function Write-ProtocolMappingReport {
    param([string]$Root)

    $enumFile = Join-Path $Root "RemoteControl.Protocals\Codec\ePacketType.cs"
    $codecFile = Join-Path $Root "RemoteControl.Protocals\Codec\CodecFactory.cs"
    $packetTypes = Get-EnumPacketTypes -EnumFile $enumFile
    $mappedTypes = Get-MappedPacketTypes -CodecFile $codecFile

    $ignored = @(
        "PACKET_HEART_BEAR",
        "PACKET_CLIENT_CLOSE_RESPONSE",
        "CYCLER_RELAY_CLIENT_LIST_REQUEST"
    )

    $unmapped = @()
    foreach ($packetType in $packetTypes) {
        if ($ignored -contains $packetType) {
            continue
        }
        if (-not ($mappedTypes -contains $packetType)) {
            $unmapped += $packetType
        }
    }

    Write-Host ""
    Write-Host "Protocol mapping report"
    Write-Host "Review-only: bodyless control packets can be intentionally unmapped."
    Write-Host ""
    if ($unmapped.Count -eq 0) {
        Write-Host "No unmapped packet types found after ignore list."
    }
    else {
        Write-Host ("Packet types without CodecFactory JSON mapping: {0}" -f $unmapped.Count)
        $unmapped | Sort-Object | ForEach-Object { Write-Host $_ }
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository: $root"
Write-CodeLengthReport -Root $root

if ($CheckProtocolMappings) {
    Write-ProtocolMappingReport -Root $root
}
