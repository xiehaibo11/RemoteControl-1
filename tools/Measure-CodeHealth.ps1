param(
    [int]$TargetMaxLines = 500,
    [int]$WarningLines = 300,
    [switch]$IncludeGenerated,
    [switch]$CheckProtocolMappings,
    [switch]$CheckProjectIncludes,
    [switch]$FailOnViolation,
    [switch]$FailOnWarnings,
    [string]$ProjectIncludeIgnoreFile = "tools\CodeHealth.ProjectIncludeIgnore.txt"
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

    $script:CodeLengthRows = $rows
    $script:CodeLengthViolationCount = @($rows | Where-Object { $_.Lines -gt $TargetMaxLines }).Count
    $script:CodeLengthWarningCount = @($rows | Where-Object { $_.Lines -gt $WarningLines }).Count

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
    Write-Host ("Files over warning: {0}" -f $script:CodeLengthWarningCount)
}

function Test-IsProjectSourceFile {
    param([string]$Path)

    if ($Path -match "\\(bin|obj)\\") {
        return $false
    }
    if ($Path -match "\\\.vs\\") {
        return $false
    }
    return $true
}

function Get-ProjectCompileIncludes {
    param([string]$ProjectFile)

    $project = New-Object System.Xml.XmlDocument
    $project.Load($ProjectFile)
    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
    $namespaceManager.AddNamespace("msb", $project.DocumentElement.NamespaceURI)

    $result = New-Object System.Collections.Generic.List[string]
    $nodes = $project.SelectNodes("//msb:Compile[@Include]", $namespaceManager)
    foreach ($node in $nodes) {
        $include = $node.Attributes["Include"].Value
        if (-not [string]::IsNullOrEmpty($include)) {
            $result.Add($include)
        }
    }
    return $result
}

function Get-ProjectIncludeIgnoreSet {
    param([string]$Root)

    $result = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    $ignoreFilePath = Join-Path $Root $ProjectIncludeIgnoreFile
    if (-not (Test-Path -LiteralPath $ignoreFilePath)) {
        return $result
    }

    foreach ($line in Get-Content -Path $ignoreFilePath -Encoding UTF8) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrEmpty($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $normalized = $trimmed.Replace("/", "\")
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $Root $normalized))
        [void]$result.Add($fullPath)
    }
    return $result
}

function Write-ProjectIncludeReport {
    param([string]$Root)

    $script:ProjectIncludeViolationCount = 0
    $ignoreSet = Get-ProjectIncludeIgnoreSet -Root $Root
    Write-Host ""
    Write-Host "Project include report"
    Write-Host "Checks legacy explicit Compile includes for missing files and uncompiled .cs files."
    Write-Host ""

    $projects = Get-ChildItem -Path $Root -Recurse -Filter "*.csproj" |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
        Sort-Object FullName

    foreach ($projectFile in $projects) {
        $projectContent = Get-Content -Path $projectFile.FullName -Raw -Encoding UTF8
        $projectRelPath = Get-RelativePath -Root $Root -Path $projectFile.FullName
        if ($projectContent -match "<Project\s+Sdk=") {
            Write-Host ("{0}: SDK-style project, implicit Compile includes skipped." -f $projectRelPath)
            continue
        }

        $projectDir = Split-Path -Parent $projectFile.FullName
        $compileIncludes = Get-ProjectCompileIncludes -ProjectFile $projectFile.FullName
        $compileSet = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
        $missingIncludes = @()

        foreach ($include in $compileIncludes) {
            $resolved = [System.IO.Path]::GetFullPath((Join-Path $projectDir $include))
            [void]$compileSet.Add($resolved)
            if (-not (Test-Path -LiteralPath $resolved)) {
                $missingIncludes += $include
            }
        }

        $sourceFiles = Get-ChildItem -Path $projectDir -Recurse -Filter "*.cs" |
            Where-Object { Test-IsProjectSourceFile -Path $_.FullName }
        $uncompiledFiles = @()
        $ignoredUncompiledFiles = @()
        foreach ($sourceFile in $sourceFiles) {
            if (-not $compileSet.Contains($sourceFile.FullName)) {
                if ($ignoreSet.Contains($sourceFile.FullName)) {
                    $ignoredUncompiledFiles += (Get-RelativePath -Root $Root -Path $sourceFile.FullName)
                    continue
                }
                $uncompiledFiles += (Get-RelativePath -Root $Root -Path $sourceFile.FullName)
            }
        }

        $projectViolations = $missingIncludes.Count + $uncompiledFiles.Count
        $script:ProjectIncludeViolationCount += $projectViolations
        if ($projectViolations -eq 0) {
            if ($ignoredUncompiledFiles.Count -gt 0) {
                Write-Host ("{0}: OK ({1} intentional exclusion(s))" -f $projectRelPath, $ignoredUncompiledFiles.Count)
            }
            else {
                Write-Host ("{0}: OK" -f $projectRelPath)
            }
            continue
        }

        Write-Host ("{0}: {1} issue(s)" -f $projectRelPath, $projectViolations)
        foreach ($missing in $missingIncludes) {
            Write-Host ("  Missing Compile Include: {0}" -f $missing)
        }
        foreach ($uncompiled in $uncompiledFiles) {
            Write-Host ("  Uncompiled source file: {0}" -f $uncompiled)
        }
        foreach ($ignored in $ignoredUncompiledFiles) {
            Write-Host ("  Ignored uncompiled source file: {0}" -f $ignored)
        }
    }

    Write-Host ""
    Write-Host ("Project include issues: {0}" -f $script:ProjectIncludeViolationCount)
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

$script:CodeLengthRows = @()
$script:CodeLengthViolationCount = 0
$script:CodeLengthWarningCount = 0
$script:ProjectIncludeViolationCount = 0

$root = Get-RepositoryRoot
Write-Host "Repository: $root"
Write-CodeLengthReport -Root $root

if ($CheckProtocolMappings) {
    Write-ProtocolMappingReport -Root $root
}

if ($CheckProjectIncludes) {
    Write-ProjectIncludeReport -Root $root
}

if ($FailOnWarnings -and $script:CodeLengthWarningCount -gt 0) {
    exit 1
}

if ($FailOnViolation -and ($script:CodeLengthViolationCount -gt 0 -or $script:ProjectIncludeViolationCount -gt 0)) {
    exit 1
}
