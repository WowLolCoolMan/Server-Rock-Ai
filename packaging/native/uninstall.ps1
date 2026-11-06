[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'Programs\Server Rock AI'),
    [switch]$RemoveUserData
)

$ErrorActionPreference = 'Stop'
$profileRoot = Join-Path $env:LOCALAPPDATA 'ServerRock\AI'
if ($PSCmdlet.ShouldProcess($InstallRoot, 'Uninstall Server Rock AI')) {
    if (Test-Path -LiteralPath $InstallRoot) { Remove-Item -LiteralPath $InstallRoot -Recurse -Force }
    if ($RemoveUserData -and (Test-Path -LiteralPath $profileRoot)) {
        $confirmation = Read-Host "Type REMOVE to delete $profileRoot"
        if ($confirmation -eq 'REMOVE') { Remove-Item -LiteralPath $profileRoot -Recurse -Force }
        else { Write-Warning 'User data was retained because confirmation did not match.' }
    }
    Write-Host "Server Rock AI binaries removed. User data is retained at $profileRoot unless explicitly confirmed for removal."
}
