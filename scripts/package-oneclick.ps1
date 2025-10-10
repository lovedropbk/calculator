param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64"
)


$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$winuiProj = Join-Path $root "winui3-mvp/FinancialCalculator.WinUI3/FinancialCalculator.WinUI3.csproj"
$outDir = Join-Path $root "artifacts/oneclick/$Runtime"

Write-Host "==> Building Go backend (fc-svc)"
& go build -v -o "$root/bin/fc-svc.exe" "$root/cmd/fc-svc" | Write-Host

Write-Host "==> Restoring and building WinUI app with MSBuild ($Configuration, $Runtime)"
& msbuild $winuiProj /p:Configuration=$Configuration /p:Platform=x64 /p:RuntimeIdentifier=$Runtime /restore:true | Write-Host

Write-Host "==> Publishing WinUI app (self-contained single-file + Windows App Runtime)"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
& dotnet publish $winuiProj -c $Configuration -r $Runtime -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:WindowsPackageType=None -o $outDir | Write-Host


Write-Host "==> Staging embedded backend"
if (!(Test-Path "$root/bin/fc-svc.exe")) { throw "fc-svc.exe missing at $root/bin/fc-svc.exe" }
Copy-Item "$root/bin/fc-svc.exe" (Join-Path $outDir "fc-svc.exe") -Force

Write-Host "==> Done"
Write-Host "Output: $outDir/FinancialCalculator.WinUI3.exe"
