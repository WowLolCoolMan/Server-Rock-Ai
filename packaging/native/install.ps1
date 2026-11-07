[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageRoot,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'Programs\Server Rock AI'),
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $PackageRoot).Path
$manifest = Join-Path $package 'release-manifest.json'
if (-not (Test-Path -LiteralPath $manifest)) { throw "release-manifest.json is missing from $package" }
$metadata = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
$version = [string]$metadata.version
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$') { throw "Invalid release version: $version" }

$versionRoot = Join-Path $InstallRoot $version
$current = Join-Path $InstallRoot 'current'
$previous = Join-Path $InstallRoot 'previous'
New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
if (Test-Path -LiteralPath $versionRoot) { throw "Version $version is already installed at $versionRoot" }

if ($PSCmdlet.ShouldProcess($versionRoot, 'Install Server Rock AI')) {
    New-Item -ItemType Directory -Force -Path $versionRoot | Out-Null
    Get-ChildItem -LiteralPath $package -Force | Copy-Item -Destination $versionRoot -Recurse -Force
    if (Test-Path -LiteralPath $current) {
        if (Test-Path -LiteralPath $previous) { Remove-Item -LiteralPath $previous -Force -Recurse }
        Move-Item -LiteralPath $current -Destination $previous
    }
    New-Item -ItemType Junction -Path $current -Target $versionRoot | Out-Null
    $shim = Join-Path $InstallRoot 'ServerRock.Native.exe'
    if (Test-Path -LiteralPath $shim) { Remove-Item -LiteralPath $shim -Force }
    Copy-Item -LiteralPath (Join-Path $versionRoot 'ServerRock.Native.exe') -Destination $shim -Force
    $startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Server Rock AI.lnk'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $startMenu) | Out-Null
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($startMenu)
    $shortcut.TargetPath = $shim
    $shortcut.WorkingDirectory = $InstallRoot
    $shortcut.Description = 'Local-first AI workspace'
    $shortcut.Save()
    Write-Host "Installed Server Rock AI $version. User data remains under $env:LOCALAPPDATA\ServerRock\AI."
    if ($Launch) { Start-Process -FilePath $shim }
}
