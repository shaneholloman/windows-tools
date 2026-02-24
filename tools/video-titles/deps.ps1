# deps.ps1 - video-titles dependencies
# Ensures the neu CLI is installed and the Neutralinojs binary is present.

$toolDir = Split-Path $MyInvocation.MyCommand.Path

# Check npm
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "  [skip] video-titles: npm not found - install Node.js from https://nodejs.org" -ForegroundColor Yellow
    return
}

# Ensure neu CLI is installed
if (-not (Get-Command neu -ErrorAction SilentlyContinue)) {
    Write-Host "  [neu]  Installing @neutralinojs/neu CLI..." -ForegroundColor Cyan
    npm install -g @neutralinojs/neu | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [fail] Failed to install neu CLI" -ForegroundColor Red
        return
    }
    Write-Host "  [ok]   neu CLI installed" -ForegroundColor Green
} else {
    Write-Host "  [ok]   neu CLI found: $(neu --version 2>$null)" -ForegroundColor Green
}

# Ensure Neutralinojs binary + client library are present (both gitignored; neu update fetches both)
$binary    = Join-Path $toolDir "bin\neutralino-win_x64.exe"
$clientLib = Join-Path $toolDir "resources\js\neutralino.js"
if (-not (Test-Path $binary) -or -not (Test-Path $clientLib)) {
    Write-Host "  [neu]  Downloading Neutralinojs binary + client library for video-titles..." -ForegroundColor Cyan
    Push-Location $toolDir
    neu update 2>&1 | Out-Null
    Pop-Location
    if ((Test-Path $binary) -and (Test-Path $clientLib)) {
        Write-Host "  [ok]   Neutralinojs files downloaded" -ForegroundColor Green
    } else {
        Write-Host "  [fail] Failed to download Neutralinojs files" -ForegroundColor Red
    }
} else {
    Write-Host "  [ok]   Neutralinojs binary and client library present" -ForegroundColor Green
}
