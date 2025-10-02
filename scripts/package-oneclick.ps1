param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$winuiProj = Join-Path $root "winui3-mvp/FinancialCalculator.WinUI3/FinancialCalculator.WinUI3.csproj"

Write-Host "==> Building Go backend (fc-svc)"
& go build -o "$root/bin/fc-svc.exe" "$root/cmd/fc-svc" | Write-Host

Write-Host "==> Publishing WinUI app ($Configuration, $Runtime)"
& dotnet publish $winuiProj -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$root/artifacts/oneclick/$Runtime"

Write-Host "==> Staging embedded backend"
Copy-Item "$root/bin/fc-svc.exe" "$root/artifacts/oneclick/$Runtime/fc-svc.exe" -Force

Write-Host "==> Done"
Write-Host "Output: $root/artifacts/oneclick/$Runtime/FinancialCalculator.WinUI3.exe"
