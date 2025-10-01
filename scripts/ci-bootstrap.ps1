param(
  [Parameter(Mandatory=$false)] [string]$RepoUrl,
  [string]$Branch = "main",
  [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = 'Stop'

function Ensure-Git {
  try { git --version | Out-Null } catch { throw "Git is not installed or not on PATH." }
}

function Init-Repo {
  if (-not (Test-Path .git)) {
    Write-Host "Initializing git repository..."
    git init | Out-Null
  } else {
    Write-Host ".git exists; using existing repository." 
  }
}

function Configure-User {
  $name = (git config user.name) 2>$null
  $email = (git config user.email) 2>$null
  if (-not $name) { git config user.name "Rovo Dev" }
  if (-not $email) { git config user.email "rovo-dev@localhost" }
}

function First-Commit {
  $status = git status --porcelain
  if (-not $status) { Write-Host "No changes to commit."; return }
  git add -A
  # Commit message is explicit and descriptive per request (ASCII only to avoid encoding issues)
  git commit -m 'feat(mvp): initial commit - WinUI 3 MVP, modular Go backends (fc-api, fc-svc), and CI workflows (build-winui3, build-go)' | Out-Null
  Write-Host 'Committed changes.' 
}

function Set-Remote {
  if (-not $RepoUrl) { Write-Host "No RepoUrl provided; skipping remote setup."; return }
  $existing = (git remote) -split "\n" | Where-Object { $_ -eq 'origin' }
  if (-not $existing) {
    Write-Host "Adding remote origin -> $RepoUrl"
    git remote add origin $RepoUrl | Out-Null
  } else {
    Write-Host "Updating remote origin -> $RepoUrl"
    git remote set-url origin $RepoUrl | Out-Null
  }
}

function Push-Branch {
  git branch -M $Branch | Out-Null
  if ($Token) {
    # Attempt token-authenticated push if PAT provided
    if ($RepoUrl -match "^https://github.com/(.+)$") {
      $ownerRepo = $Matches[1] -replace "\\.git$",""
      $authUrl = "https://$Token@github.com/$ownerRepo.git"
      Write-Host "Pushing to $Branch using token auth..."
      git push -u $authUrl $Branch
      return
    }
  }
  Write-Host "Pushing to $Branch (interactive auth via Git Credential Manager may prompt)..."
  git push -u origin $Branch
}

function Dispatch-Workflow {
  param([string]$WorkflowFile)
  if (-not $Token) { Write-Host "No PAT provided; skip dispatch for $WorkflowFile. Workflows will run on push."; return }
  if ($RepoUrl -notmatch "^https://github.com/(.+)$") { Write-Warning "Cannot parse owner/repo from $RepoUrl"; return }
  $ownerRepo = $Matches[1] -replace "\\.git$",""
  $uri = "https://api.github.com/repos/$ownerRepo/actions/workflows/$WorkflowFile/dispatches"
  $body = @{ ref = $Branch } | ConvertTo-Json
  Write-Host "Dispatching $WorkflowFile on $Branch..."
  $headers = @{ 
    Authorization = "Bearer $Token"
    Accept = "application/vnd.github+json"
    'X-GitHub-Api-Version' = '2022-11-28'
  }
  try {
    Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -Body $body
    Write-Host "$WorkflowFile dispatched."
  } catch {
    $msg = $_.Exception.Message
    Write-Warning "Dispatch failed for workflow - $msg"
  }
}

# Main
Ensure-Git
Init-Repo
Configure-User
First-Commit
Set-Remote
Push-Branch

# Dispatch workflows (optional; push already triggers them)
Dispatch-Workflow -WorkflowFile 'build-go.yml'
Dispatch-Workflow -WorkflowFile 'build-winui3.yml'

Write-Host "Done. Navigate to GitHub Actions to watch runs and download artifacts."