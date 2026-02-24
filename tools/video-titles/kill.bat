@echo off
wmic process where "CommandLine like '%%video-titles.ps1%%'" delete >nul 2>&1
exit /b 0
