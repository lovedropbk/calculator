param(
  [string]$Configuration = "Debug",
  [string]$ApiPort = "8123",
  [int]$HealthTimeoutSec = 30,
  [switch]$ForceKill
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn { param($msg) Write-Warning $msg }
function Write-Err  { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Test-Cmd   { param($name) return $null -ne (Get-Command $name -ErrorAction SilentlyContinue) }

Write-Info "Using repo root: $repoRoot"
Write-Info "Configuration: $Configuration"
Write-Info "API Port: $ApiPort"

function Stop-ByName([string]$name) {
  try { Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue } catch { }
}

function Stop-ByPort([int]$port) {
  try {
    $lines = netstat -ano | Select-String -Pattern "LISTENING\s+.*:$port\s+" | ForEach-Object { $_.ToString() }
    foreach ($line in $lines) {
      $parts = $line -split '\s+'
      $procId = $parts[-1]
      if ($procId -match '^\d+$') {
        try { Stop-Process -Id ([int]$procId) -Force -ErrorAction SilentlyContinue } catch { }
      }
    }
  } catch { }
}

if ($ForceKill) {
  Write-Info "Force killing prior processes and freeing port $ApiPort ..."
  Stop-ByName "fc-api"
  Stop-ByName "FinancialCalculator.WinUI3"
  Stop-ByPort $ApiPort
}

# Build backend
if (Test-Cmd "go") {
  try {
    Write-Info "Building Go backend: fc-api.exe"
    go build -o "$repoRoot\fc-api.exe" "$repoRoot\cmd\fc-api"
    Write-Info "Built fc-api.exe successfully"
  } catch {
    Write-Warn "Go build failed: $($_.Exception.Message). Falling back to existing fc-api.exe if present."
  }
} else {
  Write-Warn "Go is not installed or not in PATH. Skipping backend build and using existing fc-api.exe if present."
}

$backendExe = Join-Path $repoRoot "fc-api.exe"
if (-not (Test-Path $backendExe)) {
  Write-Err "Backend executable not found: $backendExe"
  exit 1
}

# Start backend with per-process env
Write-Info "Starting backend on http://127.0.0.1:$ApiPort ..."
$backendCmd = "/c set FC_API_PORT=$ApiPort && `"$backendExe`""
$backendProcShim = Start-Process -FilePath "cmd.exe" -ArgumentList $backendCmd -PassThru -WindowStyle Hidden

# Wait for health
$healthUrl = "http://127.0.0.1:$ApiPort/healthz"
$healthy = $false
$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt $HealthTimeoutSec) {
  try {
    $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2
    if ($resp.StatusCode -eq 200 -and ($resp.Content -match "ok")) {
      $healthy = $true
      break
    }
  } catch { }
  Start-Sleep -Milliseconds 300
}
$sw.Stop()

$backendReal = Get-Process -Name "fc-api" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($healthy) {
  if ($backendReal) {
    Write-Info ("Backend is healthy at {0} after {1:N1}s (PID={2})" -f $healthUrl, $sw.Elapsed.TotalSeconds, $backendReal.Id)
  } else {
    Write-Info ("Backend is healthy at {0} after {1:N1}s" -f $healthUrl, $sw.Elapsed.TotalSeconds)
  }
} else {
  Write-Warn "Backend did not become healthy within $HealthTimeoutSec seconds. Continuing anyway."
}

# Build frontend
if (-not (Test-Cmd "dotnet")) {
  Write-Err ".NET SDK (dotnet) not found in PATH."
  exit 1
}

$csproj = Join-Path $repoRoot "winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj"
Write-Info "Building frontend: $csproj"
dotnet build $csproj -c $Configuration | Out-Host

# Find frontend exe
$exeMatch = Get-ChildItem -Path (Join-Path $repoRoot "winui3-mvp\FinancialCalculator.WinUI3\bin\$Configuration") -Recurse -Filter "FinancialCalculator.WinUI3.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $exeMatch) {
  Write-Err "Could not find FinancialCalculator.WinUI3.exe under bin\$Configuration"
  exit 1
}

# Start frontend with per-process env
$apiBase = "http://127.0.0.1:$ApiPort/"
$frontendCmd = "/c set FC_API_BASE=$apiBase && `"$($exeMatch.FullName)`""
Write-Info "Launching frontend with FC_API_BASE=$apiBase"
$frontendProcShim = Start-Process -FilePath "cmd.exe" -ArgumentList $frontendCmd -PassThru -WorkingDirectory (Split-Path -Parent $exeMatch.FullName)

Start-Sleep -Milliseconds 500
$frontendReal = Get-Process -Name "FinancialCalculator.WinUI3" -ErrorAction SilentlyContinue | Select-Object -First 1

Write-Host ""
if ($backendReal -and $frontendReal) {
  Write-Info "Started:"
  Write-Host ("  Backend: fc-api.exe (PID={0}) at {1}" -f $backendReal.Id, ("http://127.0.0.1:$ApiPort")) -ForegroundColor Green
  Write-Host ("  Frontend: {0} (PID={1})" -f $exeMatch.FullName, $frontendReal.Id) -ForegroundColor Green
} else {
  Write-Info "Started (PID detection best-effort):"
  if ($backendReal) {
    Write-Host ("  Backend: fc-api.exe (PID={0}) at {1}" -f $backendReal.Id, ("http://127.0.0.1:$ApiPort")) -ForegroundColor Green
  } else {
    Write-Host ("  Backend: fc-api.exe (PID=unknown) at {0}" -f ("http://127.0.0.1:$ApiPort")) -ForegroundColor Yellow
  }
  if ($frontendReal) {
    Write-Host ("  Frontend: {0} (PID={1})" -f $exeMatch.FullName, $frontendReal.Id) -ForegroundColor Green
  } else {
    Write-Host ("  Frontend: {0} (PID=unknown)" -f $exeMatch.FullName) -ForegroundColor Yellow
  }
}

Write-Host ""
Write-Host ("To stop backend: Stop-Process -Name fc-api") -ForegroundColor Yellow
Write-Host ("To stop frontend: Stop-Process -Name FinancialCalculator.WinUI3") -ForegroundColor Yellow