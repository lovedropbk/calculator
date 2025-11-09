# This script starts mitmproxy with the email_rewrite.py addon.
# It uses $PSScriptRoot to ensure it can be run from any directory,
# as long as email_rewrite.py is in the same directory as this script.

param(
    [switch]$Background = $false,
    [int]$Port = 8080,
    [string]$UpstreamProxy
)

Write-Host "Starting mitmproxy with email_rewrite.py (Background=$Background, Port=$Port, UpstreamProxy=$UpstreamProxy)..."
$scriptPath = Join-Path $PSScriptRoot "email_rewrite.py"

function Get-MitmCommand {
    $mw = Get-Command mitmweb -ErrorAction SilentlyContinue
    if ($mw) { return "mitmweb" }
    $mp = Get-Command mitmproxy -ErrorAction SilentlyContinue
    if ($mp) { return "mitmproxy" }
    return $null
}

$mitmCmd = Get-MitmCommand
if (-not $mitmCmd) {
    Write-Error "mitmproxy/mitmweb is not installed or not on PATH. Please install: 'pip install mitmproxy'"
    exit 1
}

# Build arguments: use raw path for direct invocation; include quotes only for Start-Process string
$invokeArgs = @("-s", $scriptPath, "--set", "ssl_insecure=true", "-p", $Port)
$startArgs = @("-s", '"' + $scriptPath + '"', "--set", "ssl_insecure=true", "-p", $Port)

if ($UpstreamProxy) {
    $invokeArgs = @("-m", "upstream:$UpstreamProxy") + $invokeArgs
    $startArgs  = @("-m", "upstream:$UpstreamProxy") + $startArgs
}

if ($Background) {
    # Start in background and write PID to stdout for callers to capture
    $proc = Start-Process -FilePath $mitmCmd -ArgumentList ($startArgs -join ' ') -WindowStyle Hidden -PassThru
    Write-Host "mitmproxy started in background (PID=$($proc.Id))."
    Write-Output $proc.Id
}
else {
    & $mitmCmd @invokeArgs
}