<#
.SYNOPSIS
    C# Dead Code / Unused Feature Scanner
.DESCRIPTION
    Scans all .cs source files to find:
    1. Private/internal methods that are never called
    2. Form classes that are never instantiated
    3. onMenu* handlers not bound to any menu
    4. Handler classes that are never registered
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Find-DeadCode.ps1
#>

param(
    [string]$ProjectDir = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Continue"

# Collect all source files (exclude bin/obj)
$allFiles = Get-ChildItem -Path $ProjectDir -Recurse -Include "*.cs" -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$sourceFiles = $allFiles | Where-Object { $_.Name -notlike "*.Designer.cs" }
$designerFiles = $allFiles | Where-Object { $_.Name -like "*.Designer.cs" }

# Read all source content
$allContent = @{}
foreach ($f in $allFiles) {
    $allContent[$f.FullName] = Get-Content $f.FullName -Raw -ErrorAction SilentlyContinue
}

$allText = ($allContent.Values) -join "`n"

Write-Host "=== C# Dead Code Scanner ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectDir"
Write-Host "Source files: $($sourceFiles.Count) (+ $($designerFiles.Count) Designer)"
Write-Host ""

# ============================================================
# 1. Unused private/internal methods
# ============================================================
Write-Host "[1] Scanning unused private/internal methods..." -ForegroundColor Yellow

$methodPattern = '(?m)^\s+(?:private|internal)\s+(?:static\s+)?(?:void|bool|string|int|object|[\w<>\[\],\s]+?)\s+(\w+)\s*\('
$unusedMethods = @()

foreach ($file in $sourceFiles) {
    $content = $allContent[$file.FullName]
    if (-not $content) { continue }

    $matches = [regex]::Matches($content, $methodPattern)
    foreach ($m in $matches) {
        $methodName = $m.Groups[1].Value

        # Skip framework methods
        if ($methodName -match '^(InitializeComponent|Dispose|Main|ToString|GetHashCode|Equals|OnPaint|OnResize)$') { continue }
        if ($methodName -match '^(get_|set_)') { continue }

        # Count references in all code
        $refPattern = '\b' + [regex]::Escape($methodName) + '\b'
        $callCount = ([regex]::Matches($allText, $refPattern)).Count

        # Only definition itself = 1 occurrence
        if ($callCount -le 1) {
            $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
            $unusedMethods += [PSCustomObject]@{
                File   = $file.FullName.Replace($ProjectDir + "\", "")
                Line   = $lineNum
                Method = $methodName
            }
        }
    }
}

if ($unusedMethods.Count -gt 0) {
    Write-Host "  Found $($unusedMethods.Count) potentially unused methods:" -ForegroundColor Red
    $unusedMethods | ForEach-Object {
        Write-Host "    [$($_.Line)] $($_.File) -> $($_.Method)()" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "  No unused private methods found" -ForegroundColor Green
}

Write-Host ""

# ============================================================
# 2. Form classes never instantiated
# ============================================================
Write-Host "[2] Scanning Form classes never instantiated..." -ForegroundColor Yellow

$classPattern = '(?m)^\s+public\s+(?:partial\s+)?class\s+(Frm\w+)'
$formClasses = @()

foreach ($file in $sourceFiles) {
    $content = $allContent[$file.FullName]
    if (-not $content) { continue }

    $classMatches = [regex]::Matches($content, $classPattern)
    foreach ($cm in $classMatches) {
        $className = $cm.Groups[1].Value
        if ($formClasses -notcontains $className) {
            $formClasses += $className
        }
    }
}

$unusedForms = @()
foreach ($cls in $formClasses) {
    $instantiatePattern = 'new\s+' + [regex]::Escape($cls) + '\s*\('
    $refCount = ([regex]::Matches($allText, $instantiatePattern)).Count

    if ($refCount -eq 0) {
        $unusedForms += $cls
    }
}

if ($unusedForms.Count -gt 0) {
    Write-Host "  Found $($unusedForms.Count) Form classes never instantiated:" -ForegroundColor Red
    $unusedForms | ForEach-Object {
        Write-Host "    $_ (never new)" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "  All Form classes are instantiated" -ForegroundColor Green
}

Write-Host ""

# ============================================================
# 3. onMenu* methods not bound to any menu
# ============================================================
Write-Host "[3] Scanning onMenu* methods not bound to menus..." -ForegroundColor Yellow

$menuMethodPattern = '(?m)private\s+void\s+(onMenu\w+)\s*\('
$menuMethods = @()

foreach ($file in $sourceFiles) {
    $content = $allContent[$file.FullName]
    if (-not $content) { continue }

    $mm = [regex]::Matches($content, $menuMethodPattern)
    foreach ($m in $mm) {
        $name = $m.Groups[1].Value
        if ($menuMethods -notcontains $name) {
            $menuMethods += $name
        }
    }
}

$unboundMenus = @()
foreach ($method in $menuMethods) {
    $bindPattern = '\b' + [regex]::Escape($method) + '\b'
    $refs = ([regex]::Matches($allText, $bindPattern)).Count

    # definition only = 1 ref (method signature + body reference to itself doesn't count)
    if ($refs -le 1) {
        $unboundMenus += $method
    }
}

if ($unboundMenus.Count -gt 0) {
    Write-Host "  Found $($unboundMenus.Count) onMenu methods NOT bound to any menu:" -ForegroundColor Red
    $unboundMenus | ForEach-Object {
        Write-Host "    $_()" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "  All onMenu* methods are bound" -ForegroundColor Green
}

Write-Host ""

# ============================================================
# 4. Handler classes never registered
# ============================================================
Write-Host "[4] Scanning Handler classes never registered..." -ForegroundColor Yellow

$handlerClassPattern = '(?m)class\s+(\w+Handler)\s*[:\s{]'
$handlerClasses = @()

foreach ($file in $sourceFiles) {
    $content = $allContent[$file.FullName]
    if (-not $content) { continue }

    $hm = [regex]::Matches($content, $handlerClassPattern)
    foreach ($m in $hm) {
        $name = $m.Groups[1].Value
        if ($handlerClasses -notcontains $name) {
            $handlerClasses += $name
        }
    }
}

$unregisteredHandlers = @()
foreach ($handler in $handlerClasses) {
    $regPattern = '\b' + [regex]::Escape($handler) + '\b'
    $refs = ([regex]::Matches($allText, $regPattern)).Count
    if ($refs -le 1) {
        $unregisteredHandlers += $handler
    }
}

if ($unregisteredHandlers.Count -gt 0) {
    Write-Host "  Found $($unregisteredHandlers.Count) Handler classes possibly unregistered:" -ForegroundColor Red
    $unregisteredHandlers | ForEach-Object {
        Write-Host "    $_" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "  All Handler classes are registered" -ForegroundColor Green
}

Write-Host ""

# ============================================================
# 5. Public methods defined only once (no external callers)
# ============================================================
Write-Host "[5] Scanning public methods with no callers..." -ForegroundColor Yellow

$pubMethodPattern = '(?m)^\s+public\s+(?:static\s+)?(?:void|bool|string|int|object|[\w<>\[\],\s]+?)\s+(\w+)\s*\('
$unusedPublic = @()

foreach ($file in $sourceFiles) {
    $content = $allContent[$file.FullName]
    if (-not $content) { continue }
    # Only check non-Form files (avoid UI event handler false positives)
    if ($file.Name -match '^Frm') { continue }

    $matches = [regex]::Matches($content, $pubMethodPattern)
    foreach ($m in $matches) {
        $methodName = $m.Groups[1].Value
        if ($methodName -match '^(ToString|GetHashCode|Equals|Dispose|Main)$') { continue }
        if ($methodName.Length -lt 4) { continue }

        $refPattern = '\b' + [regex]::Escape($methodName) + '\b'
        $callCount = ([regex]::Matches($allText, $refPattern)).Count

        if ($callCount -le 1) {
            $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
            $unusedPublic += [PSCustomObject]@{
                File   = $file.FullName.Replace($ProjectDir + "\", "")
                Line   = $lineNum
                Method = $methodName
            }
        }
    }
}

if ($unusedPublic.Count -gt 0) {
    Write-Host "  Found $($unusedPublic.Count) public methods with no callers:" -ForegroundColor Red
    $unusedPublic | Select-Object -First 30 | ForEach-Object {
        Write-Host "    [$($_.Line)] $($_.File) -> $($_.Method)()" -ForegroundColor DarkYellow
    }
    if ($unusedPublic.Count -gt 30) {
        Write-Host "    ... and $($unusedPublic.Count - 30) more" -ForegroundColor DarkGray
    }
} else {
    Write-Host "  All public methods have callers" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Scan Complete ===" -ForegroundColor Cyan
