param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64"
)


$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$winuiProj = Join-Path $root "winui3-mvp/FinancialCalculator.WinUI3/FinancialCalculator.WinUI3.csproj"
$outDir = Join-Path $root "artifacts/oneclick/$Runtime"

# Skipping Go backend build; app now uses local C# engine only
Write-Host "==> Skipping backend build (using local C# engine)"
Write-Host "==> Restoring and building WinUI app ($Configuration, $Runtime)"
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
  & msbuild $winuiProj /p:Configuration=$Configuration /p:Platform=x64 /p:RuntimeIdentifier=$Runtime /restore:true | Write-Host
} else {
  & dotnet build $winuiProj -c $Configuration -r $Runtime -p:Platform=x64 | Write-Host
}

Write-Host "==> Publishing WinUI app (self-contained single-file + Windows App Runtime)"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
& dotnet publish $winuiProj -c $Configuration -r $Runtime -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -p:WindowsPackageType=None -o $outDir | Write-Host


Write-Host "==> No backend to stage (pure local engine)"
Write-Host "==> Done"
Write-Host "Output: $outDir/FinancialCalculator.WinUI3.exe"
