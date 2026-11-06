[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Executable,
    [int]$TimeoutSeconds = 20
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-ElementById([System.Windows.Automation.AutomationElement]$root, [string]$id) {
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-ElementByName([System.Windows.Automation.AutomationElement]$root, [string]$name) {
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Invoke-Element([System.Windows.Automation.AutomationElement]$element, [string]$label) {
    if ($null -eq $element) { throw "UI acceptance could not find $label." }
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

$process = Start-Process -FilePath $Executable -PassThru
try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited) { throw "Native app exited before the UI became available (code $($process.ExitCode))." }
        $window = if ($process.MainWindowHandle -ne 0) {
            [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
        }
    } while ($null -eq $window -and [DateTime]::UtcNow -lt $deadline)
    if ($null -eq $window) { throw 'Native app did not expose a top-level window in time.' }

    Invoke-Element (Find-ElementById $window 'ComposerToolsButton') 'Composer Tools'
    Invoke-Element (Find-ElementById $window 'TemporaryButton') 'Temporary Chat toggle'
    if ($null -eq (Find-ElementByName $window 'Temporary chat started · messages will not be saved.')) {
        throw 'Temporary Chat did not report a successful start.'
    }
    Invoke-Element (Find-ElementById $window 'TemporaryButton') 'Temporary Chat exit toggle'
    if ($null -eq (Find-ElementByName $window 'Temporary chat cleared and ended.')) {
        throw 'Temporary Chat did not report a successful exit.'
    }
    Write-Host 'Native UI acceptance passed: Tools disclosure and Temporary Chat on/off.'
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill() }
    }
}
