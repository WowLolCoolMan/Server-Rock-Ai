[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$PackageRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$InstallRoot,
    [switch]$Launch
)

$installer = Join-Path $PSScriptRoot 'install.ps1'
& $installer -PackageRoot $PackageRoot -InstallRoot $InstallRoot -Launch:$Launch -WhatIf:$WhatIfPreference
