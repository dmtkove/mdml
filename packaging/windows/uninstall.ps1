param(
    [string]$InstallDir = $(Join-Path $env:LOCALAPPDATA "Programs\mdml")
)

$ErrorActionPreference = "Stop"

foreach ($extension in @(".md", ".markdown")) {
    $base = "HKCU:\Software\Classes\SystemFileAssociations\$extension\shell\mdmlConvert"
    if (Test-Path $base) {
        Remove-Item -Recurse -Force $base
    }
}

if (Test-Path $InstallDir) {
    Remove-Item -Recurse -Force $InstallDir
}

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not [string]::IsNullOrEmpty($userPath)) {
    $newPath = ($userPath -split ";" | Where-Object { $_ -and $_.Trim() -ne "" -and $_ -ne $InstallDir }) -join ";"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
}

Write-Host "mdml has been uninstalled."
