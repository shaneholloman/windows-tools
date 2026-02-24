@echo off
setlocal
cd /d "%~dp0"

where neu >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: neu CLI not found. Install with: npm install -g @neutralinojs/neu
    pause
    exit /b 1
)

echo Starting video-titles in dev mode (hot reload)...
neu run
