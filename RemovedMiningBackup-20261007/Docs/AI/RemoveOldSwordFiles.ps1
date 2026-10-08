param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot -ErrorAction Stop).Path.TrimEnd('\')
if (-not (Test-Path -LiteralPath (Join-Path $resolvedRoot 'Assets')) -or
    -not (Test-Path -LiteralPath (Join-Path $resolvedRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Pass the Unity project root, not the Assets folder.'
}
$obsolete = @(
    'Assets/Scripts/Ores/Editor/LowSwordStanceSetup.cs',
    'Assets/Scripts/Ores/Editor/PlayerWeaponSetup.cs',
    'Assets/Scripts/Ores/Editor/WeaponVfxSetup.cs',
    'Assets/GameData/Weapons/Sword.asset',
    'Assets/GameData/Weapons/LowSwordCombat.controller',
    'Assets/FX/WeaponFeedback/SwordStrike.prefab',
    'Assets/FX/WeaponFeedback/SwordImpact.prefab'
)
$backupRoot = Join-Path $resolvedRoot ('RemovedSwordBackup-' + [Guid]::NewGuid().ToString('N'))
foreach ($relative in $obsolete) {
    foreach ($candidate in @($relative, ($relative + '.meta'))) {
        $source = [IO.Path]::GetFullPath((Join-Path $resolvedRoot $candidate))
        if (-not $source.StartsWith(($resolvedRoot + '\Assets\'), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Invalid removal target.'
        }
        if (Test-Path -LiteralPath $source) {
            $destination = Join-Path $backupRoot $candidate
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
            Move-Item -LiteralPath $source -Destination $destination -ErrorAction Stop
        }
    }
}
Write-Output ('Old sword files archived to ' + $backupRoot)
