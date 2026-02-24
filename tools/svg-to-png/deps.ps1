$toolDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Get-Command bun -ErrorAction SilentlyContinue)) {
    Write-Host "  [svg-to-png] ERROR: bun is not installed. Install from https://bun.sh" -ForegroundColor Red
    exit 1
}

Write-Host "  [svg-to-png] Installing npm dependencies..." -ForegroundColor Cyan
Push-Location $toolDir
bun install
Pop-Location
Write-Host "  [svg-to-png] Dependencies ready." -ForegroundColor Green
