[CmdletBinding()]
param([string]$EnvironmentPath = (Join-Path $env:LOCALAPPDATA 'Skai\AirLLM'))
$ErrorActionPreference = 'Stop'
$python = (Get-Command python -ErrorAction Stop).Source
if (-not (Test-Path $EnvironmentPath)) { New-Item -ItemType Directory -Path $EnvironmentPath -Force | Out-Null }
$venv = Join-Path $EnvironmentPath 'venv'
if (-not (Test-Path (Join-Path $venv 'Scripts\python.exe'))) { & $python -m venv $venv }
$venvPython = Join-Path $venv 'Scripts\python.exe'
& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install -r (Join-Path $PSScriptRoot 'requirements.txt')
Write-Output "Skai AirLLM runtime installed. Set AirLLM Python executable to: $venvPython"
