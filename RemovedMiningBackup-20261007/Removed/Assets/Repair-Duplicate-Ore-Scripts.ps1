param(
    [string]$ProjectRoot = (Split-Path -Parent $MyInvocation.MyCommand.Path)
)

$ErrorActionPreference = 'Stop'
$projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot)
$assets = Join-Path $projectRootFull 'Assets'
if (-not (Test-Path -LiteralPath $assets -PathType Container)) {
    throw "No Assets folder at $assets. Extract this ZIP into the Unity project root."
}

$targets = @(
    @{ Name = 'Ore'; Path = 'Assets/Scripts/Ores/Ore/Ore.cs' },
    @{ Name = 'JuicyMinerProgress'; Path = 'Assets/Scripts/Ores/Npc/JuicyMinerProgress.cs' }
)
$allScripts = @(Get-ChildItem -LiteralPath $assets -Recurse -File -Filter '*.cs')
$toMove = New-Object 'System.Collections.Generic.List[object]'

foreach ($target in $targets) {
    $canonical = Join-Path $projectRootFull ($target.Path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $canonical -PathType Leaf)) {
        throw "The main script is missing: $canonical. No files were moved."
    }

    $escaped = [regex]::Escape($target.Name)
    $declaration = '(?m)\bclass\s+' + $escaped + '\b'
    $mainText = [System.IO.File]::ReadAllText($canonical)
    if ([regex]::Matches($mainText, $declaration).Count -ne 1) {
        throw "Unexpected declaration count in $canonical. No files were moved."
    }

    foreach ($script in $allScripts) {
        if ([string]::Equals($script.FullName, $canonical,
            [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        $contents = [System.IO.File]::ReadAllText($script.FullName)
        if ($contents -notmatch 'namespace\s+MiningSimulator\.Ores\b') { continue }
        if ($contents -notmatch $declaration) { continue }
        if ([regex]::Matches($contents, '(?m)\bclass\s+\w+\b').Count -ne 1) {
            throw "Duplicate file contains other classes; inspect manually: $($script.FullName). No files were moved."
        }
        $toMove.Add($script.FullName)
    }
}

if ($toMove.Count -eq 0) {
    Write-Host 'No duplicate Ore/JuicyMinerProgress classes found under Assets.'
    Write-Host 'If Unity still reports CS0101, search the full project for another declaration.'
    exit 0
}

# Keep files and GUID sidecars together, outside Assets and outside the project.
$projectParent = Split-Path -Parent $projectRootFull
$backup = Join-Path $projectParent ('UnityDuplicateScriptsBackup_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($source in $toMove) {
    $relative = $source.Substring($projectRootFull.TrimEnd('\', '/').Length).TrimStart('\', '/')
    $destination = Join-Path $backup $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Move-Item -LiteralPath $source -Destination $destination
    if (Test-Path -LiteralPath ($source + '.meta') -PathType Leaf) {
        Move-Item -LiteralPath ($source + '.meta') -Destination ($destination + '.meta')
    }
    Write-Host "Moved duplicate: $relative"
}

Write-Host "Preserved original files and .meta at: $backup"
Write-Host 'Return to Unity and let it recompile. The canonical files were kept.'
