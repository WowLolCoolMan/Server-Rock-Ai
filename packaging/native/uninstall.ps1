[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$InstallRoot,
    [switch]$RemoveUserData
)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'release-manifest.json'
if ([string]::IsNullOrWhiteSpace($InstallRoot) -and (Test-Path -LiteralPath $manifest)) {
    $metadata = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
    $folder = [string]$metadata.installFolder
    if ($folder -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { throw "Invalid install folder: $folder" }
    $InstallRoot = Join-Path $env:LOCALAPPDATA (Join-Path 'Programs' $folder)
}
if ([string]::IsNullOrWhiteSpace($InstallRoot)) { throw 'InstallRoot is required when release-manifest.json is unavailable.' }
$profileRoot = Join-Path $env:LOCALAPPDATA 'ServerRock\AI'
if ($PSCmdlet.ShouldProcess($InstallRoot, 'Uninstall the installed release')) {
    if (Test-Path -LiteralPath $InstallRoot) { Remove-Item -LiteralPath $InstallRoot -Recurse -Force }
    if ($RemoveUserData -and (Test-Path -LiteralPath $profileRoot)) {
        $confirmation = Read-Host "Type REMOVE to delete $profileRoot"
        if ($confirmation -eq 'REMOVE') { Remove-Item -LiteralPath $profileRoot -Recurse -Force }
        else { Write-Warning 'User data was retained because confirmation did not match.' }
    }
    Write-Host "Installed binaries removed. User data is retained at $profileRoot unless explicitly confirmed for removal."
}
