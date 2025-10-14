@echo off
:: Financial Calculator Launcher with Self-Contained Runtime
:: This packages all required SDKs and runtime dependencies

setlocal enabledelayedexpansion

set CONFIG=Release
set BUILD_FLAG=0
set NO_LAUNCH=0

:: Parse arguments
:parse_args
if "%1"=="" goto args_done
if /I "%1"=="Debug" set CONFIG=Debug
if /I "%1"=="Release" set CONFIG=Release
if /I "%1"=="/Build" set BUILD_FLAG=1
if /I "%1"=="/NoLaunch" set NO_LAUNCH=1
shift
goto parse_args
:args_done

echo ==> Financial Calculator Launcher
echo Configuration: %CONFIG%

:: Kill any existing instances
echo Stopping any existing instances...
taskkill /F /IM FinancialCalculator.WinUI3.exe >nul 2>&1

:: Define paths
set PROJECT_PATH=.\winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj
set PUBLISH_PATH=.\winui3-mvp\FinancialCalculator.WinUI3\bin\%CONFIG%\net8.0-windows10.0.22621.0\win-x64\publish

:: Check if we need to build
if "%BUILD_FLAG%"=="1" goto do_publish
if not exist "%PUBLISH_PATH%\FinancialCalculator.WinUI3.exe" goto do_publish
goto skip_publish

:do_publish
:: Publish with self-contained runtime
echo Publishing application with self-contained runtime...
echo This includes all necessary SDKs and runtime components.

:: Clean previous publish
if exist "%PUBLISH_PATH%" rd /s /q "%PUBLISH_PATH%"

:: Publish with all dependencies included
echo Running publish command...
dotnet publish "%PROJECT_PATH%" ^
    -c %CONFIG% ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:PublishReadyToRun=true ^
    -p:WindowsPackageType=None ^
    -p:WindowsAppSDKSelfContained=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%PUBLISH_PATH%"

if errorlevel 1 (
    echo Publish failed! Trying fallback build...
    
    :: Fallback to regular build
    dotnet build "%PROJECT_PATH%" -c %CONFIG%
    
    if errorlevel 1 (
        echo Build also failed!
        pause
        exit /b 1
    )
)

:skip_publish

:: Find the executable
set EXE_PATH=%PUBLISH_PATH%\FinancialCalculator.WinUI3.exe

if not exist "%EXE_PATH%" (
    :: Try build output if publish failed
    echo Published exe not found, trying build output...
    set EXE_PATH=.\winui3-mvp\FinancialCalculator.WinUI3\bin\%CONFIG%\net8.0-windows10.0.22621.0\win-x64\FinancialCalculator.WinUI3.exe
    
    if not exist "!EXE_PATH!" (
        echo Could not find FinancialCalculator.WinUI3.exe
        pause
        exit /b 1
    )
)

:: Get working directory
for %%F in ("%EXE_PATH%") do set WORK_DIR=%%~dpF

:: Verify Windows App SDK runtime files
echo Checking for required runtime files...
set MISSING_COUNT=0

if not exist "%WORK_DIR%Microsoft.WindowsAppRuntime.Bootstrap.dll" (
    echo   - Missing: Microsoft.WindowsAppRuntime.Bootstrap.dll
    set /a MISSING_COUNT+=1
)
if not exist "%WORK_DIR%Microsoft.Windows.SDK.NET.dll" (
    echo   - Missing: Microsoft.Windows.SDK.NET.dll
    set /a MISSING_COUNT+=1
)
if not exist "%WORK_DIR%WinRT.Runtime.dll" (
    echo   - Missing: WinRT.Runtime.dll
    set /a MISSING_COUNT+=1
)

if %MISSING_COUNT% GTR 0 (
    echo Warning: Some runtime files may be missing!
    echo The app may require Windows App SDK Runtime to be installed.
    echo Download from: https://aka.ms/windowsappsdk/1.4/latest/windowsappruntimeinstall-x64.exe
    echo.
)

if "%NO_LAUNCH%"=="1" goto no_launch

:: Launch the app
echo Launching Financial Calculator...
echo Executable: %EXE_PATH%
echo Working Directory: %WORK_DIR%
cd /d "%WORK_DIR%"
start "" "%EXE_PATH%"
cd /d "%~dp0\.."

echo.
echo Application launched successfully!
echo The app is using the local C# calculation engine.
echo.
echo To stop the app: Close the window or run:
echo   taskkill /F /IM FinancialCalculator.WinUI3.exe
goto end

:no_launch
echo.
echo Application built/published successfully!
echo Executable location: %EXE_PATH%

:end
echo.
echo Tips:
echo   - Use /Build flag to force rebuild/republish
echo   - Use /NoLaunch flag to build without launching
echo   - Use Debug or Release as first argument for configuration
echo.

endlocal