# deps.ps1 - install dependencies for video-description

if (-not (Get-Command bun -ErrorAction SilentlyContinue)) {
    Write-Host "  [WARN] bun is not installed." -ForegroundColor Yellow
    Write-Host "         Install it with:  winget install oven-sh.bun" -ForegroundColor Yellow
    return
}

Write-Host "  [bun]  Installing video-description dependencies..." -ForegroundColor DarkGray
Push-Location $PSScriptRoot
bun install --silent 2>&1 | Out-Null
Pop-Location
Write-Host "  [ok]   video-description dependencies ready." -ForegroundColor Green
