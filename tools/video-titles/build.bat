@echo off
setlocal
cd /d "%~dp0"

where neu >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: neu CLI not found.
    echo Install it with: npm install -g @neutralinojs/neu
    pause
    exit /b 1
)

echo Building video-titles...
neu build

if %errorlevel% neq 0 (
    echo Build FAILED.
    pause
    exit /b 1
)

echo.
echo Build succeeded: dist\video-titles\
