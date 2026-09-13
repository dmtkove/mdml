param(
    [string]$InstallDir = $(Join-Path $env:LOCALAPPDATA "Programs\mdml")
)

$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot "mdml.exe"
if (-not (Test-Path $source)) {
    throw "mdml.exe was not found next to install.ps1"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Force $source (Join-Path $InstallDir "mdml.exe")

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ([string]::IsNullOrEmpty($userPath)) {
    $userPath = ""
}
$pathParts = $userPath -split ";" | Where-Object { $_ -and $_.Trim() -ne "" }
if ($pathParts -notcontains $InstallDir) {
    $newPath = ($pathParts + $InstallDir) -join ";"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
}

function Register-MdmlVerb([string]$extension) {
    $base = "HKCU:\Software\Classes\SystemFileAssociations\$extension\shell\mdmlConvert"
    New-Item -Path $base -Force | Out-Null
    Set-ItemProperty -Path $base -Name "(default)" -Value "Convert to HTML"
    New-Item -Path "$base\command" -Force | Out-Null
    $exe = Join-Path $InstallDir "mdml.exe"
    Set-ItemProperty -Path "$base\command" -Name "(default)" -Value "`"$exe`" `"%1`""
}

Register-MdmlVerb ".md"
Register-MdmlVerb ".markdown"

Write-Host "Installed mdml to $InstallDir"
Write-Host "Open a new terminal, then run: mdml README.md"
Write-Host "In Explorer, right-click a Markdown file and choose Convert to HTML."
