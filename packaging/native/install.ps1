[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageRoot,
    [string]$InstallRoot,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $PackageRoot).Path
$manifest = Join-Path $package 'release-manifest.json'
if (-not (Test-Path -LiteralPath $manifest)) { throw "release-manifest.json is missing from $package" }
$metadata = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
$version = [string]$metadata.version
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$') { throw "Invalid release version: $version" }
$displayName = [string]$metadata.displayName
$installFolder = [string]$metadata.installFolder
if ([string]::IsNullOrWhiteSpace($displayName) -or $displayName.Length -gt 128) { throw 'The release manifest has no valid display name.' }
if ($installFolder -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { throw "Invalid install folder: $installFolder" }
if ([string]::IsNullOrWhiteSpace($InstallRoot)) { $InstallRoot = Join-Path $env:LOCALAPPDATA (Join-Path 'Programs' $installFolder) }

$versionRoot = Join-Path $InstallRoot $version
$current = Join-Path $InstallRoot 'current'
$previous = Join-Path $InstallRoot 'previous'
New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
if (Test-Path -LiteralPath $versionRoot) { throw "Version $version is already installed at $versionRoot" }

if ($PSCmdlet.ShouldProcess($versionRoot, "Install $displayName")) {
    New-Item -ItemType Directory -Force -Path $versionRoot | Out-Null
    Get-ChildItem -LiteralPath $package -Force | Copy-Item -Destination $versionRoot -Recurse -Force
    if (Test-Path -LiteralPath $current) {
        if (Test-Path -LiteralPath $previous) { Remove-Item -LiteralPath $previous -Force -Recurse }
        Move-Item -LiteralPath $current -Destination $previous
    }
    New-Item -ItemType Junction -Path $current -Target $versionRoot | Out-Null
    $launcher = Join-Path $current 'ServerRock.Native.exe'
    if (-not (Test-Path -LiteralPath $launcher)) { throw "The installed package does not contain $($metadata.artifact)." }
    $startMenu = Join-Path $env:APPDATA (Join-Path 'Microsoft\Windows\Start Menu\Programs' "$displayName.lnk")
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $startMenu) | Out-Null
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($startMenu)
    $shortcut.TargetPath = $launcher
    $shortcut.WorkingDirectory = $current
    $shortcut.Description = $displayName
    $shortcut.Save()
    Write-Host "Installed $displayName $version. User data remains under $env:LOCALAPPDATA\ServerRock\AI."
    if ($Launch) { Start-Process -FilePath $launcher -WorkingDirectory $current }
}
