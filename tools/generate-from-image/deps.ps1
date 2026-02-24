# deps.ps1 - install dependencies for generate-from-image

if (-not (Get-Command bun -ErrorAction SilentlyContinue)) {
    Write-Host "  [WARN] bun is not installed." -ForegroundColor Yellow
    Write-Host "         Install it with:  winget install oven-sh.bun" -ForegroundColor Yellow
    Write-Host "         Or visit:         https://bun.sh" -ForegroundColor Yellow
    return
}

Write-Host "  [bun]  Installing generate-from-image dependencies..." -ForegroundColor DarkGray
Push-Location $PSScriptRoot
bun install --silent 2>&1 | Out-Null
Pop-Location
Write-Host "  [ok]   generate-from-image dependencies ready." -ForegroundColor Green
