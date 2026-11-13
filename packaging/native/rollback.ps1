[CmdletBinding(SupportsShouldProcess)]
param([string]$InstallRoot)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'release-manifest.json'
if ([string]::IsNullOrWhiteSpace($InstallRoot) -and (Test-Path -LiteralPath $manifest)) {
    $metadata = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
    $folder = [string]$metadata.installFolder
    if ($folder -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { throw "Invalid install folder: $folder" }
    $InstallRoot = Join-Path $env:LOCALAPPDATA (Join-Path 'Programs' $folder)
}
if ([string]::IsNullOrWhiteSpace($InstallRoot)) { throw 'InstallRoot is required when release-manifest.json is unavailable.' }
$current = Join-Path $InstallRoot 'current'
$previous = Join-Path $InstallRoot 'previous'
if (-not (Test-Path -LiteralPath $previous)) { throw 'No previous release is available for rollback.' }
if ($PSCmdlet.ShouldProcess($InstallRoot, 'Roll back the installed release')) {
    $currentTarget = (Get-Item -LiteralPath $current).Target
    Remove-Item -LiteralPath $current -Force
    Move-Item -LiteralPath $previous -Destination $current
    Move-Item -LiteralPath $currentTarget -Destination $previous
    Write-Host "Rolled back to $((Get-Item -LiteralPath $current).Target). User data was not changed."
}
