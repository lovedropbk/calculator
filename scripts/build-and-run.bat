@echo off
setlocal enableextensions enabledelayedexpansion

REM Configuration
set CONFIG=Debug
set API_PORT=8123

REM Allow overrides via args: build-and-run.bat [Debug|Release] [port]
if /I "%~1"=="" (goto :argsdone) else (set CONFIG=%~1)
if /I "%~2"=="" (goto :argsdone) else (set API_PORT=%~2)
:argsdone

REM Move to repo root (parent of scripts)
pushd "%~dp0\.."

echo [INFO] Repo root: %CD%
echo [INFO] Configuration: %CONFIG%
echo [INFO] API Port: %API_PORT%

REM Kill any stray processes (best-effort)
for %%P in (fc-api FinancialCalculator.WinUI3) do (
  taskkill /F /IM %%P.exe >nul 2>&1
)

REM Build backend if Go is available
where go >nul 2>&1
if %ERRORLEVEL%==0 (
  echo [INFO] Building Go backend: fc-api.exe
  go build -o fc-api.exe .\cmd\fc-api
  if errorlevel 1 (
    echo [WARN] Go build failed. Attempting to use existing fc-api.exe if present.
  ) else (
    echo [INFO] Built fc-api.exe successfully
  )
) else (
  echo [WARN] Go is not installed or not in PATH. Using existing fc-api.exe if present.
)

if not exist fc-api.exe (
  echo [ERROR] Backend executable not found: %CD%\fc-api.exe
  popd
  exit /b 1
)

REM Start backend with per-process environment
echo [INFO] Starting backend on http://127.0.0.1:%API_PORT% ...
start "fc-api" cmd /c "set FC_API_PORT=%API_PORT% && fc-api.exe"

REM Health check loop (PowerShell-based)
set HEALTH_OK=0
for /L %%i in (1,1,60) do (
  powershell -NoProfile -Command "try { $r=Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 http://127.0.0.1:%API_PORT%/healthz; if ($r.StatusCode -eq 200 -and $r.Content -match 'ok') { exit 0 } else { exit 1 } } catch { exit 1 }"
  if not errorlevel 1 (
    set HEALTH_OK=1
    goto :healthdone
  )
  ping -n 1 127.0.0.1 >nul
)
:healthdone

if "%HEALTH_OK%"=="1" (
  echo [INFO] Backend is healthy at http://127.0.0.1:%API_PORT%/healthz
) else (
  echo [WARN] Backend did not become healthy in time. Continuing anyway.
)

REM Build frontend
echo [INFO] Building frontend: winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj
dotnet build ".\winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj" -c %CONFIG%
if errorlevel 1 (
  echo [ERROR] Frontend build failed.
  popd
  exit /b 1
)

REM Locate frontend exe (use known path for WinUI3)
set "EXE=.\winui3-mvp\FinancialCalculator.WinUI3\bin\%CONFIG%\net8.0-windows10.0.22621.0\win-x64\FinancialCalculator.WinUI3.exe"
if not exist "%EXE%" (
  echo [ERROR] Frontend exe not found: %EXE%
  popd
  exit /b 1
)

REM Launch frontend with per-process environment
echo [INFO] Launching frontend with FC_API_BASE=http://127.0.0.1:%API_PORT%/
start "" cmd /c "set FC_API_BASE=http://127.0.0.1:%API_PORT%/ && \"%EXE%\""

echo.
echo [INFO] Started:
echo   Backend: fc-api.exe at http://127.0.0.1:%API_PORT%
echo   Frontend: %EXE%
echo.
echo [INFO] To stop: taskkill /F /IM fc-api.exe ^& taskkill /F /IM FinancialCalculator.WinUI3.exe

popd
endlocal