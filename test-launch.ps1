param(
    [switch]$UseOneClick
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "==> Stopping any existing processes..." -ForegroundColor Yellow
Get-Process -Name "fc-svc","fc-api","FinancialCalculator.WinUI3" -ErrorAction SilentlyContinue | Stop-Process -Force

if ($UseOneClick) {
    Write-Host "==> Testing one-click package..." -ForegroundColor Cyan
    $exePath = ".\artifacts\oneclick\win-x64\FinancialCalculator.WinUI3.exe"
    if (-not (Test-Path $exePath)) {
        Write-Host "One-click package not found at $exePath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "==> Launching one-click executable..." -ForegroundColor Green
    Start-Process -FilePath $exePath
    
} else {
    Write-Host "==> Starting backend (fc-svc) on port 8123..." -ForegroundColor Cyan
    $backend = Start-Process -FilePath ".\fc-svc.exe" -PassThru -WindowStyle Hidden
    
    Write-Host "==> Waiting for backend health check..." -ForegroundColor Yellow
    $healthy = $false
    for ($i = 0; $i -lt 30; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri "http://localhost:8123/healthz" -UseBasicParsing -TimeoutSec 1
            if ($resp.StatusCode -eq 200) {
                $healthy = $true
                Write-Host "==> Backend is healthy!" -ForegroundColor Green
                break
            }
        } catch {}
        Start-Sleep -Milliseconds 500
    }
    
    if (-not $healthy) {
        Write-Host "Backend failed to become healthy!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "==> Finding frontend executable..." -ForegroundColor Cyan
    $frontend = Get-ChildItem -Path ".\winui3-mvp\FinancialCalculator.WinUI3\bin" -Recurse -Filter "FinancialCalculator.WinUI3.exe" | 
                Sort-Object LastWriteTime -Descending | 
                Select-Object -First 1
    
    if (-not $frontend) {
        Write-Host "Frontend executable not found! Building..." -ForegroundColor Yellow
        dotnet build .\winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj -c Debug
        $frontend = Get-ChildItem -Path ".\winui3-mvp\FinancialCalculator.WinUI3\bin" -Recurse -Filter "FinancialCalculator.WinUI3.exe" | 
                    Sort-Object LastWriteTime -Descending | 
                    Select-Object -First 1
    }
    
    Write-Host "==> Starting frontend with FC_API_BASE=http://localhost:8123/..." -ForegroundColor Cyan
    $env:FC_API_BASE = "http://localhost:8123/"
    Start-Process -FilePath $frontend.FullName -WorkingDirectory (Split-Path $frontend.FullName -Parent)
}

Write-Host ""
Write-Host "==> Application launched successfully!" -ForegroundColor Green
Write-Host "    Backend API: http://localhost:8123/" -ForegroundColor Cyan
Write-Host "    Frontend: WinUI3 Application Window" -ForegroundColor Cyan
Write-Host ""
Write-Host "To stop all processes, run:" -ForegroundColor Yellow
Write-Host "    Get-Process -Name 'fc-svc','FinancialCalculator.WinUI3' | Stop-Process -Force" -ForegroundColor White