param(
  [Parameter(Mandatory = $true)]
  [string]$RepoUrl,

  [string]$Branch = "main",
  [string]$CommitMessage = "feat: add keyauth desktop panel"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot

try {
  $existingOrigin = ""
  try {
    $existingOrigin = (git remote get-url origin 2>$null).Trim()
  } catch {
    $existingOrigin = ""
  }

  if ($existingOrigin) {
    git remote set-url origin $RepoUrl
  } else {
    git remote add origin $RepoUrl
  }

  git add -A
  $hasChanges = (git status --porcelain)
  if ($hasChanges) {
    git commit -m $CommitMessage | Out-Null
  } else {
    Write-Host "Nenhuma alteracao pendente para commit."
  }

  git branch -M $Branch
  git push -u origin $Branch
} finally {
  Pop-Location
}
