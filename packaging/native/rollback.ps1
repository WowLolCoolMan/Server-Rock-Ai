[CmdletBinding(SupportsShouldProcess)]
param([string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'Programs\Server Rock AI'))

$ErrorActionPreference = 'Stop'
$current = Join-Path $InstallRoot 'current'
$previous = Join-Path $InstallRoot 'previous'
if (-not (Test-Path -LiteralPath $previous)) { throw "No previous Server Rock AI version is available for rollback." }
if ($PSCmdlet.ShouldProcess($InstallRoot, 'Roll back Server Rock AI')) {
    $currentTarget = (Get-Item -LiteralPath $current).Target
    Remove-Item -LiteralPath $current -Force
    Move-Item -LiteralPath $previous -Destination $current
    Move-Item -LiteralPath $currentTarget -Destination $previous
    $shim = Join-Path $InstallRoot 'ServerRock.Native.exe'
    if (Test-Path -LiteralPath $shim) { Remove-Item -LiteralPath $shim -Force }
    Copy-Item -LiteralPath (Join-Path $current 'ServerRock.Native.exe') -Destination $shim -Force
    Write-Host "Rolled back to $((Get-Item -LiteralPath $current).Target). User data was not changed."
}
