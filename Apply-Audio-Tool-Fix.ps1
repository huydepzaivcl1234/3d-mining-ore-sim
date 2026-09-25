$ErrorActionPreference = 'Stop'
$package = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = $package
for ($depth = 0; $depth -lt 5; $depth++) {
    if (Test-Path -LiteralPath (Join-Path $project 'Assets/Scripts/Ores/Editor/MiningAudioToolWindow.cs') -PathType Leaf) { break }
    $parent = Split-Path -Parent $project
    if ($parent -eq $project) { break }
    $project = $parent
}
$target = Join-Path $project 'Assets/Scripts/Ores/Editor/MiningAudioToolWindow.cs'
if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
    throw 'MiningAudioToolWindow.cs not found. Extract ZIP inside your Unity project root or Assets folder.'
}
$before = [IO.File]::ReadAllText($target)
$normalize = { param($value) $value.Replace("`r`n", "`n") }
$reference = [IO.File]::ReadAllText((Join-Path $package 'Source/MiningAudioToolWindow.original.cs.txt'))
$fixed = [IO.File]::ReadAllText((Join-Path $package 'Source/MiningAudioToolWindow.cs.txt'))
if ($before.Contains('MiningOreSetupMenu.CreateOrUpdateStarterOres();')) {
    if ((& $normalize $before) -ceq (& $normalize $reference)) {
        $result = $fixed
        Write-Host 'Installed updated Audio Manager tool with optional MiningAudioData creation.'
    } else {
        # Preserve local customizations; remove only the deleted setup-menu dependency.
        $result = $before.Replace('MiningOreSetupMenu.CreateOrUpdateStarterOres();', '')
        $result = $result.Replace('Setup / Cập nhật Managers và HUD', 'Nạp lại MiningAudioData')
        $result = $result.Replace('Chưa có MiningAudioData. Hãy bấm nút Setup bên dưới.',
            'Chưa có MiningAudioData. Tạo tại Assets > Create > Mining Simulator > Game Data > Audio.')
        Write-Host 'Preserved your customized Audio Manager tool; removed missing setup-menu call.'
    }
    [IO.File]::WriteAllText($target, $result, (New-Object Text.UTF8Encoding($false)))
} elseif ($before.Contains('MiningAudioData') -and -not $before.Contains('MiningOreSetupMenu')) {
    Write-Host 'No broken MiningOreSetupMenu call remains; no file changed.'
} else {
    throw 'Unexpected Audio Manager tool content; no file changed.'
}
Write-Host 'Unity project:' $project
Write-Host 'Reopen Unity and check Console compilation.'
