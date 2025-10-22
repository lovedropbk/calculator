# Financial Calculator Tests Launcher
$ErrorActionPreference = "Stop"
$projectPath = ".\tests\FinancialCalculator.Tests\FinancialCalculator.Tests.csproj"
$publishPath = ".\tests\FinancialCalculator.Tests\bin\Debug\net8.0-windows10.0.22621.0\win-x64\publish"

Write-Host "Publishing tests with self-contained runtime..." -ForegroundColor Yellow
# Added -p:EnableWindowsTargeting=true just in case
dotnet publish $projectPath -c Debug -r win-x64 --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:EnableWindowsTargeting=true -o $publishPath

if ($LASTEXITCODE -eq 0) {
    $exePath = Join-Path $publishPath "FinancialCalculator.Tests.exe"
    if (Test-Path $exePath) {
        Write-Host "Running tests..." -ForegroundColor Green
        & $exePath
    } else {
        Write-Host "Executable not found at $exePath" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}