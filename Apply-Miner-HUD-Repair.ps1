$ErrorActionPreference = 'Stop'
$package = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = $package
for ($depth = 0; $depth -lt 5; $depth++) {
    if (Test-Path -LiteralPath (Join-Path $project 'Assets/Scripts/Ores/Editor/MiningPcHudCleanLayoutMenu.cs') -PathType Leaf) { break }
    $parent = Split-Path -Parent $project
    if ($parent -eq $project) { throw 'Unity project not found. Extract this ZIP in the project root or Assets folder.' }
    $project = $parent
}
$menu = Join-Path $project 'Assets/Scripts/Ores/Editor/MiningPcHudCleanLayoutMenu.cs'
if (-not (Test-Path -LiteralPath $menu)) {
    throw 'MiningPcHudCleanLayoutMenu.cs not found. Install this patch in the matching version of the project.'
}
$source = Join-Path $package 'Source/MiningPcHudProgressTextRepair.cs.txt'
$sourceMeta = Join-Path $package 'Source/MiningPcHudProgressTextRepair.cs.meta.txt'
$destination = Join-Path $project 'Assets/Scripts/Ores/Editor/MiningPcHudProgressTextRepair.cs'
if (-not (Test-Path -LiteralPath $source)) { throw 'Repair source is missing from ZIP.' }

# The previous duplicated scripts prevent any Unity Editor menu from compiling.
# Move duplicates to a folder outside Unity and preserve the canonical GUIDs first.
& (Join-Path $package 'Remove-Duplicate-Ore-Scripts.ps1')

$marker = 'if (presenter != null) SetReference(presenter, "compactPcXpLabel", null);'
$call = 'MiningPcHudProgressTextRepair.RepairPanel(panel);'
$body = [System.IO.File]::ReadAllText($menu)
if (-not $body.Contains($call)) {
    if (-not $body.Contains($marker)) { throw "Cannot find the expected compact HUD setup in $menu. No HUD scripts were changed." }
    $body = $body.Replace($marker, $marker + [Environment]::NewLine + '            ' + $call)
    [System.IO.File]::WriteAllText($menu, $body, (New-Object System.Text.UTF8Encoding($false)))
}
Copy-Item -LiteralPath $source -Destination $destination -Force
if (-not (Test-Path -LiteralPath ($destination + '.meta'))) {
    Copy-Item -LiteralPath $sourceMeta -Destination ($destination + '.meta')
}
Write-Host ''
Write-Host 'HUD scripts installed. Open Unity, wait for compilation, then run:'
Write-Host 'Mining Simulator > UI > Repair Miner Progress Text'
Write-Host 'Save the scene in Unity after reviewing the editable labels.'
