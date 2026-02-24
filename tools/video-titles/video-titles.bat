@echo off
setlocal
if "%~1"=="" (
    echo Usage: video-titles ^<video_file^>
    exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0video-titles.ps1" "%~1"
