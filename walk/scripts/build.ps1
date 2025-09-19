Param(
  [switch]$Clean
)

# Ensure we're at repo root and compute walk dir
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$walkDir   = Split-Path -Parent $scriptDir     # ...\financial_calculator\walk
$repo      = Split-Path -Parent $walkDir       # ...\financial_calculator (repo root)
Set-Location $repo

# Folders
$binDir   = Join-Path $walkDir "bin"
$buildDir = Join-Path $walkDir "build\windows"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

if ($Clean) {
  Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $repo "resource.syso")
  Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $walkDir "cmd\fc-walk\resource.syso")
  Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $binDir "fc-walk.exe")
}

# Resource generation (manifest + version metadata)
# Requires goversioninfo: go install github.com/josephspurrier/goversioninfo/cmd/goversioninfo@latest
$goversioninfo = Join-Path $env:USERPROFILE "go\bin\goversioninfo.exe"
if (-not (Test-Path $goversioninfo)) {
  Write-Host "Installing goversioninfo (first-time setup) ..."
  go install github.com/josephspurrier/goversioninfo/cmd/goversioninfo@latest
}

$versionInfoPath = Join-Path $walkDir "versioninfo.json"
if (-not (Test-Path $versionInfoPath)) {
  Write-Error "Missing walk\versioninfo.json. Aborting."
  exit 1
}

$manifestPath = Join-Path $walkDir "build\windows\walk.exe.manifest"
if (-not (Test-Path $manifestPath)) {
  Write-Error "Missing walk\build\windows\walk.exe.manifest. Aborting."
  exit 1
}

$iconPath = Join-Path $repo "build\windows\icon.ico"
if (-not (Test-Path $iconPath)) {
  Write-Warning "Icon not found at build\windows\icon.ico. Continuing without icon."
}

Push-Location $repo
$sysoTarget = Join-Path $walkDir "cmd\fc-walk\resource.syso"
if (Test-Path $iconPath) {
  & $goversioninfo -ver $versionInfoPath -manifest $manifestPath -icon $iconPath -o $sysoTarget
} else {
  & $goversioninfo -ver $versionInfoPath -manifest $manifestPath -o $sysoTarget
}
Pop-Location

# Build the native GUI binary with Walk entrypoint (build subdir to avoid legacy mains)
$exePath = Join-Path $binDir "fc-walk.exe"
# Disable pointer checking to fix lxn/walk compatibility issue with newer Go versions
go build -tags walkui -gcflags=all=-d=checkptr=0 -ldflags "-H=windowsgui" -o $exePath ./walk/cmd/fc-walk

if ($LASTEXITCODE -ne 0) {
  Write-Error "Build failed."
  exit 1
}

Copy-Item -Force $manifestPath ($exePath + ".manifest")

Write-Host "Build succeeded:" $exePath