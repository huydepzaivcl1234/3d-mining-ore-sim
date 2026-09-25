$ErrorActionPreference = 'Stop'
$start = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = $start
if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets/Scripts/Ores') -PathType Container)) {
    $project = Split-Path -Parent $start
}
if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets/Scripts/Ores') -PathType Container)) {
    throw 'Place both repair files at the Unity project root (beside Assets) and run the CMD file.'
}
$assets = Join-Path $project 'Assets'
$targets = @(
    @{ Name = 'Ore'; Relative = 'Assets/Scripts/Ores/Ore/Ore.cs'; Meta = 'Ore.cs.meta' },
    @{ Name = 'JuicyMinerProgress'; Relative = 'Assets/Scripts/Ores/Npc/JuicyMinerProgress.cs'; Meta = 'JuicyMinerProgress.cs.meta' }
)
$scripts = @(Get-ChildItem -LiteralPath $assets -Recurse -File -Filter '*.cs')
$plan = New-Object 'System.Collections.Generic.List[object]'

foreach ($target in $targets) {
    $canonical = Join-Path $project ($target.Relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    $escaped = [regex]::Escape($target.Name)
    $declaration = '(?m)\bclass\s+' + $escaped + '\b'
    $canonicalText = if (Test-Path -LiteralPath $canonical) {
        [System.IO.File]::ReadAllText($canonical)
    } else { '' }
    $copies = @($scripts | Where-Object {
        if ([string]::Equals($_.FullName, $canonical, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
        $body = [System.IO.File]::ReadAllText($_.FullName)
        return $body -match 'namespace\s+MiningSimulator\.Ores\b' -and $body -match $declaration
    })

    if ([string]::IsNullOrWhiteSpace($canonicalText)) {
        if ($copies.Count -eq 0) {
            $fallback = Join-Path $start ('PreservedSource/' + $target.Name + '.cs.txt')
            if (-not (Test-Path -LiteralPath $fallback)) {
                throw "No source found to restore $($target.Name). Nothing was moved."
            }
        }
        $preferred = @($copies | Where-Object {
            $_.FullName -like ('*' + [IO.Path]::DirectorySeparatorChar + 'Assets' +
                [IO.Path]::DirectorySeparatorChar + 'Assets' + [IO.Path]::DirectorySeparatorChar + '*')
        })
        $source = if ($preferred.Count -gt 0) { $preferred[0].FullName }
                  elseif ($copies.Count -gt 0) { $copies[0].FullName }
                  else { $fallback }
        $replacement = [System.IO.File]::ReadAllText($source)
        if ([regex]::Matches($replacement, $declaration).Count -ne 1) {
            throw "Source has multiple class definitions: $source. Nothing was moved."
        }
        $plan.Add(@{ Action = 'Restore'; Source = $source; Destination = $canonical; Meta = $target.Meta })
    } elseif ([regex]::Matches($canonicalText, $declaration).Count -ne 1) {
        throw "Canonical file has unexpected class definitions: $canonical. Nothing was moved."
    }

    foreach ($copy in $copies) {
        $body = [System.IO.File]::ReadAllText($copy.FullName)
        if ([regex]::Matches($body, '(?m)\bclass\s+\w+\b').Count -ne 1) {
            throw "Extra classes in duplicate: $($copy.FullName). Nothing was moved."
        }
        $plan.Add(@{ Action = 'Quarantine'; Source = $copy.FullName })
    }
}

if ($plan.Count -eq 0) {
    Write-Host 'No duplicate Ore or JuicyMinerProgress class found under Assets.'
    return
}

$backup = Join-Path (Split-Path -Parent $project) (
    'UnityDuplicateScriptsBackup_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' +
    [guid]::NewGuid().ToString('N').Substring(0, 6))
New-Item -ItemType Directory -Path $backup -Force | Out-Null

foreach ($item in $plan | Where-Object { $_.Action -eq 'Restore' }) {
    $parent = Split-Path -Parent $item.Destination
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $item.Destination -Force
    $metaPath = $item.Destination + '.meta'
    if (-not (Test-Path -LiteralPath $metaPath)) {
        Copy-Item -LiteralPath (Join-Path $start ('PreservedMetas/' + $item.Meta)) -Destination $metaPath
    }
    Write-Host "Restored canonical script: $($item.Destination)"
}

foreach ($item in $plan | Where-Object { $_.Action -eq 'Quarantine' }) {
    $relative = $item.Source.Substring($project.TrimEnd('\', '/').Length).TrimStart('\', '/')
    $destination = Join-Path $backup $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Move-Item -LiteralPath $item.Source -Destination $destination
    if (Test-Path -LiteralPath ($item.Source + '.meta')) {
        Move-Item -LiteralPath ($item.Source + '.meta') -Destination ($destination + '.meta')
    }
    Write-Host "Moved duplicate: $relative"
}

foreach ($target in $targets) {
    $remaining = @(Get-ChildItem -LiteralPath $assets -Recurse -File -Filter '*.cs' |
        Where-Object {
            $body = [System.IO.File]::ReadAllText($_.FullName)
            $body -match 'namespace\s+MiningSimulator\.Ores\b' -and
                $body -match ('(?m)\bclass\s+' + [regex]::Escape($target.Name) + '\b')
        })
    if ($remaining.Count -ne 1) {
        throw "Expected one $($target.Name) script; found $($remaining.Count). Backup: $backup"
    }
    Write-Host "Verified one $($target.Name) class: $($remaining[0].FullName)"
}

Write-Host "Backup outside Unity project: $backup"
Write-Host 'Open Unity again and wait for compilation.'
