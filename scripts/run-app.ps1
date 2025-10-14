# Financial Calculator Launcher with Self-Contained Runtime
# This packages all required SDKs and runtime dependencies

param(
    [string]$Configuration = "Release",
    [switch]$Build,
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"

Write-Host "==> Financial Calculator Launcher" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Kill any existing instances
Write-Host "Stopping any existing instances..." -ForegroundColor Yellow
Get-Process -Name "FinancialCalculator.WinUI3" -ErrorAction SilentlyContinue | Stop-Process -Force

# Define paths
$projectPath = ".\winui3-mvp\FinancialCalculator.WinUI3\FinancialCalculator.WinUI3.csproj"
$publishPath = ".\winui3-mvp\FinancialCalculator.WinUI3\bin\$Configuration\net8.0-windows10.0.22621.0\win-x64\publish"

if ($Build -or !(Test-Path $publishPath)) {
    # Publish with self-contained runtime
    Write-Host "Publishing application with self-contained runtime..." -ForegroundColor Yellow
    Write-Host "This includes all necessary SDKs and runtime components." -ForegroundColor Gray
    
    # Clean previous publish (best effort, may be locked)
    if (Test-Path $publishPath) {
        try {
            Remove-Item -Path $publishPath -Recurse -Force -ErrorAction Stop
        } catch {
            Write-Host "Warning: Could not clean previous publish folder (may be in use)" -ForegroundColor Yellow
        }
    }
    
    # Publish with all dependencies included
    $publishArgs = @(
        "publish"
        $projectPath
        "-c", $Configuration
        "-r", "win-x64"
        "--self-contained", "true"
        "-p:PublishSingleFile=false"
        "-p:PublishReadyToRun=true"
        "-p:WindowsPackageType=None"
        "-p:WindowsAppSDKSelfContained=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-o", $publishPath
    )
    
    Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    & dotnet $publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!" -ForegroundColor Red
        Write-Host "Trying fallback build..." -ForegroundColor Yellow
        
        # Fallback to regular build
        dotnet build $projectPath -c $Configuration
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build also failed!" -ForegroundColor Red
            exit 1
        }
    }
}

# Find the executable
$exePath = Join-Path $publishPath "FinancialCalculator.WinUI3.exe"

if (-not (Test-Path $exePath)) {
    # Try build output if publish failed
    Write-Host "Published exe not found, trying build output..." -ForegroundColor Yellow
    $exePath = ".\winui3-mvp\FinancialCalculator.WinUI3\bin\$Configuration\net8.0-windows10.0.22621.0\win-x64\FinancialCalculator.WinUI3.exe"
    
    if (-not (Test-Path $exePath)) {
        # Last resort - search for any exe
        $exePath = Get-ChildItem -Path ".\winui3-mvp\FinancialCalculator.WinUI3\bin\$Configuration" -Recurse -Filter "FinancialCalculator.WinUI3.exe" | Select-Object -First 1
        if ($exePath) {
            $exePath = $exePath.FullName
        } else {
            Write-Host "Could not find FinancialCalculator.WinUI3.exe" -ForegroundColor Red
            exit 1
        }
    }
}

# Verify Windows App SDK runtime files are present
$workDir = Split-Path -Parent $exePath
$requiredFiles = @(
    "Microsoft.WindowsAppRuntime.Bootstrap.dll",
    "Microsoft.Windows.SDK.NET.dll",
    "WinRT.Runtime.dll",
    "Microsoft.InteractiveExperiences.Projection.dll"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $workDir $file
    if (-not (Test-Path $filePath)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "Warning: Some runtime files may be missing:" -ForegroundColor Yellow
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    Write-Host "The app may require Windows App SDK Runtime to be installed." -ForegroundColor Yellow
    Write-Host "Download from: https://aka.ms/windowsappsdk/1.4/latest/windowsappruntimeinstall-x64.exe" -ForegroundColor Cyan
}

if (!$NoLaunch) {
    # Launch the app
    Write-Host "Launching Financial Calculator..." -ForegroundColor Green
    Write-Host "Executable: $exePath" -ForegroundColor Gray
    Write-Host "Working Directory: $workDir" -ForegroundColor Gray
    
    Start-Process -FilePath $exePath -WorkingDirectory $workDir
    
    Write-Host ""
    Write-Host "Application launched successfully!" -ForegroundColor Green
    Write-Host "The app is using the local C# calculation engine." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To stop the app: Close the window or run:" -ForegroundColor Yellow
    Write-Host "  Stop-Process -Name FinancialCalculator.WinUI3" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Application built/published successfully!" -ForegroundColor Green
    Write-Host "Executable location: $exePath" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Tips:" -ForegroundColor Yellow
Write-Host "  - Use -Build flag to force rebuild/republish" -ForegroundColor Gray
Write-Host "  - Use -NoLaunch flag to build without launching" -ForegroundColor Gray
Write-Host "  - Use -Configuration Debug for debug build" -ForegroundColor Gray