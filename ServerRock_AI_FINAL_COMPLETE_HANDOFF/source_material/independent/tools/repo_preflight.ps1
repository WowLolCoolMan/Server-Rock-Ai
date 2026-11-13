param([string]$Repo = '.')
$ErrorActionPreference = 'Continue'
$root = (Resolve-Path -LiteralPath $Repo).Path
Write-Host "Read-only audit: $root"
Push-Location $root
try {
  git rev-parse HEAD
  git status --short
  dotnet --version
  @('scripts/audit-native-ui.cjs','packaging/native/install.ps1','native/SkaiDesktop.Core/Services/GitHubWorkspaceService.cs','native/ServerRock.Native/MainPage.xaml') | ForEach-Object { Write-Host "$_ : $(Test-Path -LiteralPath $_)" }
  Write-Host 'No repository changes were made.'
} finally { Pop-Location }
