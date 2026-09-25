$ErrorActionPreference = 'Stop'
$package = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = $package
for ($depth = 0; $depth -lt 6; $depth++) {
    if (Test-Path -LiteralPath (Join-Path $project 'Assets/Scripts/Ores/Ore/OreClickInput.cs') -PathType Leaf) { break }
    $parent = Split-Path -Parent $project
    if ($parent -eq $project) { break }
    $project = $parent
}
$inputFile = Join-Path $project 'Assets/Scripts/Ores/Ore/OreClickInput.cs'
if (-not (Test-Path -LiteralPath $inputFile -PathType Leaf)) {
    throw 'Unity project not found. Extract this ZIP into the Unity project root or Assets folder.'
}
$expectedPrefabs = @('Assets/Prefabs/Ores/Chest/ChestV1.prefab',
    'Assets/Prefabs/Ores/Chest/ChestV2.prefab')
foreach ($relative in $expectedPrefabs) {
    if (-not (Test-Path -LiteralPath (Join-Path $project $relative))) {
        throw "Chest prefab missing: $relative. No scripts installed."
    }
}
$marker = 'LuckyBlock luckyBlock = hit.collider.GetComponentInParent<LuckyBlock>();'
$inputText = [IO.File]::ReadAllText($inputFile)
if (-not $inputText.Contains($marker) -and -not $inputText.Contains('GetComponentInParent<MiningChest>()')) {
    throw 'OreClickInput.cs no longer matches the project version. No scripts installed.'
}
$mapping = @(
    @{ Folder = 'Assets/Scripts/Ores/Chest'; Name = 'MiningChest.cs' },
    @{ Folder = 'Assets/Scripts/Ores/Chest'; Name = 'MiningChestSpawner.cs' },
    @{ Folder = 'Assets/Scripts/Ores/Editor'; Name = 'MiningChestSetupMenu.cs' }
)
# Preflight before writing: never replace a locally customized script.
foreach ($entry in $mapping) {
    $source = Join-Path $package ('Source/' + $entry.Name + '.txt')
    $destination = Join-Path $project ($entry.Folder + '/' + $entry.Name)
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing ZIP source: $source" }
    if ((Test-Path -LiteralPath $destination) -and
        [IO.File]::ReadAllText($destination) -cne [IO.File]::ReadAllText($source)) {
        throw "A different $destination already exists. Nothing was changed."
    }
}
foreach ($entry in $mapping) {
    $source = Join-Path $package ('Source/' + $entry.Name + '.txt')
    $destination = Join-Path $project ($entry.Folder + '/' + $entry.Name)
    $folder = Split-Path -Parent $destination
    if (-not (Test-Path -LiteralPath $folder)) {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
    }
    if (-not (Test-Path -LiteralPath $destination)) {
        Copy-Item -LiteralPath $source -Destination $destination
        Copy-Item -LiteralPath ($source + '.meta') -Destination ($destination + '.meta')
        Write-Host "Installed $($entry.Name)"
    }
}
if (-not $inputText.Contains('GetComponentInParent<MiningChest>()')) {
    $insertion = @'
MiningChest chest = hit.collider.GetComponentInParent<MiningChest>();
            if (chest != null)
            {
                chest.MineOnce();
                return;
            }

            
'@
    $inputText = $inputText.Replace($marker, $insertion + $marker)
    [IO.File]::WriteAllText($inputFile, $inputText, (New-Object Text.UTF8Encoding($false)))
    Write-Host 'Connected chest clicks to OreClickInput.cs'
}
Write-Host 'In Unity: Mining Simulator > Tools > Setup Independent Chests'
Write-Host 'Configure Chest System in the scene and ChestV2 reward weights on the prefab, then save the scene.'
