Param(
  [switch]$Clean
)

# Ensure we're at repo root
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent $root
Set-Location $repo

# Folders
$walkDir = Join-Path $repo "walk"
$binDir = Join-Path $walkDir "bin"
$buildDir = Join-Path $walkDir "build\windows"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

if ($Clean) {
  Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $repo "resource.syso")
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
if (Test-Path $iconPath) {
  & $goversioninfo -manifest $manifestPath -icon $iconPath -o "resource.syso"
} else {
  & $goversioninfo -manifest $manifestPath -o "resource.syso"
}
Pop-Location

# Build the native GUI binary with Walk entrypoint (build subdir to avoid legacy mains)
$exePath = Join-Path $binDir "fc-walk.exe"
go build -tags walkui -ldflags "-H=windowsgui" -o $exePath ./walk/cmd/fc-walk

if ($LASTEXITCODE -ne 0) {
  Write-Error "Build failed."
  exit 1
}

Write-Host "Build succeeded:" $exePath