param(
  [string]$AppDir = "artifacts/oneclick/win-x64",
  [int]$BackendPort = 8123,
  [int]$TimeoutSec = 90,
  [switch]$KillOnExit
)

$ErrorActionPreference = 'Stop'

function Wait-HttpOk {
  param([string]$Url, [int]$TimeoutSec = 30)
  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $deadline) {
    try { $r = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 5; if ($r.StatusCode -eq 200) { return $true } } catch {}
    Start-Sleep -Milliseconds 500
  }
  return $false
}

function Get-UiLogFile {
  $logDir = Join-Path $env:LOCALAPPDATA "FinancialCalculator/logs"
  if (!(Test-Path $logDir)) { return $null }
  Get-ChildItem $logDir -Filter 'ui-*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { $_.FullName }
}

Write-Host "==> One-Click UI Smoke Test" -ForegroundColor Cyan
$AppDir = Resolve-Path $AppDir | Select-Object -ExpandProperty Path

# Stop any running instances
Write-Host "==> Stopping any existing processes..."
Get-Process -Name 'fc-svc','FinancialCalculator.WinUI3' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Validate package contents
$svcPath = Join-Path $AppDir 'fc-svc.exe'
$uiPath  = Join-Path $AppDir 'FinancialCalculator.WinUI3.exe'
if (!(Test-Path $svcPath)) { throw "Missing fc-svc.exe in $AppDir" }
if (!(Test-Path $uiPath))  { throw "Missing FinancialCalculator.WinUI3.exe in $AppDir" }

# Start backend
Write-Host "==> Starting backend (fc-svc) on port $BackendPort..."
$svcProc = Start-Process -FilePath $svcPath -WorkingDirectory $AppDir -PassThru
$base = "http://localhost:$BackendPort"
if (-not (Wait-HttpOk -Url ("$base/healthz") -TimeoutSec 30)) { throw "Backend health check failed at $base/healthz" }
Write-Host "==> Backend is healthy: $base"

# Launch UI pointing to backend
Write-Host "==> Launching UI..."
$env:FC_API_BASE = $base
$beforeLog = Get-UiLogFile
$uiProc = Start-Process -FilePath $uiPath -WorkingDirectory $AppDir -PassThru
Start-Sleep -Seconds 2

# Find latest UI log (created by Logger.Init("ui"))
$logFile = $null
$deadline = (Get-Date).AddSeconds(10)
while ((Get-Date) -lt $deadline) {
  $logFile = Get-UiLogFile
  if ($logFile -and ($beforeLog -ne $logFile)) { break }
  Start-Sleep -Milliseconds 500
}
if (-not $logFile) { Write-Warning "UI log file not found. Continuing without log capture." }
else { Write-Host "==> UI log: $logFile" }

# Allow UI to run startup sequence
Write-Host "==> Waiting for UI API activity..."
Start-Sleep -Seconds 5

# Summarize backend API smoke checks that UI would also trigger
Write-Host "==> Backend API smokes" -ForegroundColor Yellow
function PostJson($path,$obj){ $json=$obj|ConvertTo-Json -Depth 12; Invoke-WebRequest -UseBasicParsing -Method Post -Uri ($base+$path) -Body $json -ContentType 'application/json' }
$dealBase = @{ market='TH'; currency='THB'; product='HP'; price_ex_tax=1000000; down_payment_amount=100000; down_payment_percent=0.1; down_payment_locked='amount'; financed_amount=900000; term_months=48; balloon_percent=0; balloon_amount=0; timing='arrears'; rate_mode='fixed_rate'; customer_nominal_rate=0.035; target_installment=0 }

$rParams = Invoke-WebRequest -UseBasicParsing -Uri ("$base/api/v1/parameters/current"); Write-Host ("  parameters/current: {0}" -f $rParams.StatusCode)
$rAuto   = Invoke-WebRequest -UseBasicParsing -Uri ("$base/api/v1/commission/auto?product=HP"); Write-Host ("  commission/auto: {0}" -f $rAuto.StatusCode)
$rCat    = Invoke-WebRequest -UseBasicParsing -Uri ("$base/api/v1/campaigns/catalog"); $cat = $rCat.Content | ConvertFrom-Json; Write-Host ("  catalog: {0} items" -f $cat.Count)

$rCalc1  = PostJson '/api/v1/calculate' @{ deal=$dealBase; campaigns=@(); idc_items=@(); options=@{ derive_idc_from_cf=$true } }; $n1 = $rCalc1.Content | ConvertFrom-Json; Write-Host ("  calculate: MI={0} Eff={1}" -f $n1.quote.monthly_installment,$n1.quote.customer_rate_effective)
$rSum    = PostJson '/api/v1/campaigns/summaries' @{ deal=$dealBase; state=@{ dealerCommission=@{ mode='auto' }; idcOther=@{ value=0; userEdited=$false }; budgetTHB=50000 }; campaigns=($cat | ForEach-Object { @{ id=$_.id; type=$_.type; parameters=$_.parameters; eligibility=$_.eligibility; funder=$_.funder; stacking=0 } }) };
$sum = $rSum.Content | ConvertFrom-Json; Write-Host ("  summaries: {0} rows" -f $sum.Count)

# UI log analysis
$errors = @(); $warnings = @(); $apiSeen = @{}
if ($logFile -and (Test-Path $logFile)) {
  $lines = Get-Content -Path $logFile -Raw -ErrorAction SilentlyContinue
  if ($lines) {
    if ($lines -match "\[ERROR\]" -or $lines -match "\[FATAL\]") { $errors += "Errors found in UI log" }
    if ($lines -match "API Request: GET api/v1/parameters/current") { $apiSeen['params'] = $true }
    if ($lines -match "API Request: GET api/v1/commission/auto") { $apiSeen['auto'] = $true }
    if ($lines -match "API Request: POST api/v1/campaigns/summaries") { $apiSeen['sum'] = $true }
  } else { $warnings += "UI log empty" }
}

# Result summary
Write-Host "==> Summary" -ForegroundColor Cyan
if ($errors.Count -eq 0 -and $sum.Count -gt 0 -and $n1.quote.monthly_installment -gt 0) {
  Write-Host "PASS: UI + backend smoke succeeded" -ForegroundColor Green
} else {
  Write-Host "FAIL: Issues detected" -ForegroundColor Red
}

Write-Host (" - Backend: {0}" -f $base)
if ($logFile) { Write-Host (" - UI log: {0}" -f $logFile) }
if ($apiSeen.Keys.Count -gt 0) { Write-Host (" - UI API calls seen: {0}" -f ($apiSeen.Keys -join ',')) }
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Host (" - Error: {0}" -f $_) -ForegroundColor Red } }
if ($warnings.Count -gt 0) { $warnings | ForEach-Object { Write-Host (" - Warning: {0}" -f $_) -ForegroundColor Yellow } }

# Optional: stop processes after test
if ($KillOnExit) {
  Write-Host "==> Stopping processes..."
  Get-Process -Name 'FinancialCalculator.WinUI3' -ErrorAction SilentlyContinue | Stop-Process -Force
  Get-Process -Name 'fc-svc' -ErrorAction SilentlyContinue | Stop-Process -Force
}
