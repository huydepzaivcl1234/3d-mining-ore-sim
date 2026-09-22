$ErrorActionPreference = "Stop"

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

$projectRoot = (Get-Location).Path
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot ".git"))) {
    throw "Run this script from the Unity project root (the folder containing .git)."
}

Write-Host "Fetching the clean remote main..." -ForegroundColor Cyan
Invoke-Git fetch origin main

Write-Host "Rebuilding local main on top of origin/main while preserving project edits..." -ForegroundColor Cyan
Invoke-Git reset --soft origin/main

# Use both the index and the filesystem so backup folders are removed even if one side is missing.
$trackedBackupRoots = @(& git ls-files "Library_backup_*") |
    ForEach-Object { ($_ -replace '\\', '/') -split '/' | Select-Object -First 1 }
$diskBackupRoots = @(Get-ChildItem -LiteralPath $projectRoot -Directory -Filter "Library_backup_*" |
    Select-Object -ExpandProperty Name)
$backupRoots = @($trackedBackupRoots + $diskBackupRoots) |
    Where-Object { $_ -match '^Library_backup_[A-Za-z0-9._-]+$' } |
    Sort-Object -Unique

foreach ($backupRoot in $backupRoots) {
    Write-Host "Removing generated cache: $backupRoot" -ForegroundColor Yellow
    & git rm -r --cached --ignore-unmatch -- $backupRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Could not remove $backupRoot from the Git index."
    }

    $fullPath = Join-Path $projectRoot $backupRoot
    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

# Keep the current remote ignore rules, including Library_backup* protection.
Invoke-Git restore --source=origin/main --staged --worktree -- .gitignore

Write-Host "`nFiles that will be committed:" -ForegroundColor Cyan
Invoke-Git diff --cached --stat

$answer = Read-Host "Type PUSH to commit these cleaned changes to main"
if ($answer -cne "PUSH") {
    Write-Host "Stopped before commit. Your files remain staged for review." -ForegroundColor Yellow
    exit 0
}

Invoke-Git commit -m "Update without generated Unity Library backups"
Invoke-Git push --set-upstream origin main

Write-Host "Cleanup pushed successfully." -ForegroundColor Green
Write-Host "After opening Unity successfully, you may run: git lfs prune" -ForegroundColor DarkGray
