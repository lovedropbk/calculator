# Temporary localization validator - will be removed after run
param()

$ErrorActionPreference = 'Stop'

# Map of control types to expected default content properties
$expectedProps = @{
  "TextBlock"   = "Text"
  "Button"      = "Content"
  "MenuFlyoutItem" = "Text"
  "ToggleSwitch"   = @("OnContent","OffContent")
  "Expander"    = "Header"
  "InfoBar"     = "Title"
}

function Get-UidsFromXaml {
  param([string]$file)
  $content = Get-Content $file -Raw
  $results = @()
  # crude regex for elements with x:Uid
  $pattern = '<(?<tag>[A-Za-z0-9:\.]+)[^>]*x:Uid="(?<uid>[^"]+)"'
  foreach ($m in [regex]::Matches($content, $pattern)) {
    $results += [pscustomobject]@{ File=$file; Tag=$m.Groups['tag'].Value; Uid=$m.Groups['uid'].Value }
  }
  return $results
}

function Get-ReswKeys {
  param([string]$resw)
  $content = Get-Content $resw -Raw
  $pattern = '<data name="(?<name>[^\"]+)"'
  $keys = @()
  foreach ($m in [regex]::Matches($content, $pattern)) { $keys += $m.Groups['name'].Value }
  return $keys
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$xamlFiles = Get-ChildItem "$repoRoot/winui3-mvp/FinancialCalculator.WinUI3" -Recurse -Filter *.xaml |
  Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" } |
  % { $_.FullName }
$resFiles  = Get-ChildItem "$repoRoot/winui3-mvp/FinancialCalculator.WinUI3/Strings" -Recurse -Filter *.resw | % { $_.FullName }

$allKeys = @{}
foreach ($rf in $resFiles) {
  $allKeys[$rf] = Get-ReswKeys $rf
}

$issues = @()
foreach ($xf in $xamlFiles) {
  foreach ($item in Get-UidsFromXaml $xf) {
    $tag = ($item.Tag -split ':')[-1]
    if ($expectedProps.ContainsKey($tag)) {
      $props = $expectedProps[$tag]
      if (-not ($props -is [System.Array])) { $props = @($props) }
      $ok = $false
      foreach ($resw in $resFiles) {
        foreach ($p in $props) {
          if ($allKeys[$resw] -contains "$($item.Uid).$p") { $ok = $true; break }
        }
        if ($ok) { break }
      }
      if (-not $ok) {
        $issues += [pscustomobject]@{ File=$item.File; Tag=$tag; Uid=$item.Uid; Expected=$props -join '|' }
      }
    }
  }
}

if ($issues.Count -gt 0) {
  Write-Host "Found potential localization key mismatches:" -ForegroundColor Yellow
  $issues | Format-Table -AutoSize
  exit 1
}

Write-Host "Localization validation passed." -ForegroundColor Green
exit 0
